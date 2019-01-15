using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NBitcoin;
using NBitcoin.BuilderExtensions;
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;
using Stratis.Bitcoin.Base;
using Stratis.Bitcoin.Connection;
using Stratis.Bitcoin.Consensus;
using Stratis.Bitcoin.Features.Consensus;
using Stratis.Bitcoin.Features.Consensus.CoinViews;
using Stratis.Bitcoin.Features.Consensus.Interfaces;
using Stratis.Bitcoin.Features.Consensus.Rules.CommonRules;
using Stratis.Bitcoin.Features.MemoryPool;
using Stratis.Bitcoin.Features.MemoryPool.Interfaces;
using Stratis.Bitcoin.Features.Wallet.Interfaces;
using Stratis.Bitcoin.Interfaces;
using Stratis.Bitcoin.Mining;
using Stratis.Bitcoin.Utilities;

namespace Stratis.Bitcoin.Features.Miner.Staking
{
    public class DeStreamPosMinting : PosMinting
    {
        private DeStreamNetwork DeStreamNetwork
        {
            get
            {
                if (!(this.network is DeStreamNetwork))
                {
                    throw new NotSupportedException($"Network must be {nameof(NBitcoin.DeStreamNetwork)}");
                }

                return (DeStreamNetwork) this.network;
            }
        }

        protected override void CoinstakeWorker(CoinstakeWorkerContext context, ChainedHeader chainTip, Block block,
            long minimalAllowedTime, long searchInterval)
        {
            // Adds empty output for DeStream fees to the end of outputs' array

            base.CoinstakeWorker(context, chainTip, block, minimalAllowedTime, searchInterval);

            if (context.Result.KernelFoundIndex != context.Index)
            {
                return;
            }

            Script deStreamAddressKey = new KeyId(new uint160(Encoders.Base58Check
                .DecodeData(this.DeStreamNetwork.DeStreamWallet)
                .Skip(this.network.Base58Prefixes[(int) Base58Type.PUBKEY_ADDRESS].Length).ToArray())).ScriptPubKey;

            context.CoinstakeContext.CoinstakeTx.AddOutput(new TxOut(Money.Zero, deStreamAddressKey));
        }

        /// <inheritdoc/>
        public override async Task<bool> CreateCoinstakeAsync(List<UtxoStakeDescription> utxoStakeDescriptions,
            Block block, ChainedHeader chainTip, long searchInterval, long fees, CoinstakeContext coinstakeContext)
        {
            // Provides PrepareCoinStakeTransactions with fees amount

            coinstakeContext.CoinstakeTx.Inputs.Clear();
            coinstakeContext.CoinstakeTx.Outputs.Clear();

            // Mark coinstake transaction.
            coinstakeContext.CoinstakeTx.Outputs.Add(new TxOut(Money.Zero, new Script()));

            long balance = (await this.GetMatureBalanceAsync(utxoStakeDescriptions).ConfigureAwait(false)).Satoshi;
            if (balance <= this.targetReserveBalance)
            {
                this.rpcGetStakingInfoModel.PauseStaking();

                this.logger.LogTrace(
                    "Total balance of available UTXOs is {0}, which is less than or equal to reserve balance {1}.",
                    balance, this.targetReserveBalance);
                this.logger.LogTrace("(-)[BELOW_RESERVE]:false");
                return false;
            }

            // Select UTXOs with suitable depth.
            List<UtxoStakeDescription> stakingUtxoDescriptions = await this
                .GetUtxoStakeDescriptionsSuitableForStakingAsync(utxoStakeDescriptions, chainTip,
                    coinstakeContext.CoinstakeTx.Time, balance - this.targetReserveBalance).ConfigureAwait(false);
            if (!stakingUtxoDescriptions.Any())
            {
                this.rpcGetStakingInfoModel.PauseStaking();

                this.logger.LogTrace("(-)[NO_SELECTION]:false");
                return false;
            }

            long ourWeight = stakingUtxoDescriptions.Sum(s => s.TxOut.Value);
            long expectedTime = StakeValidator.TargetSpacingSeconds * this.networkWeight / ourWeight;
            decimal ourPercent = this.networkWeight != 0
                ? 100.0m * (decimal) ourWeight / (decimal) this.networkWeight
                : 0;

            this.logger.LogInformation(
                "Node staking with {0} ({1:0.00} % of the network weight {2}), est. time to find new block is {3}.",
                new Money(ourWeight), ourPercent, new Money(this.networkWeight), TimeSpan.FromSeconds(expectedTime));

            this.rpcGetStakingInfoModel.ResumeStaking(ourWeight, expectedTime);

            long minimalAllowedTime = chainTip.Header.Time + 1;
            this.logger.LogTrace(
                "Trying to find staking solution among {0} transactions, minimal allowed time is {1}, coinstake time is {2}.",
                stakingUtxoDescriptions.Count, minimalAllowedTime, coinstakeContext.CoinstakeTx.Time);

            // If the time after applying the mask is lower than minimal allowed time,
            // it is simply too early for us to mine, there can't be any valid solution.
            if ((coinstakeContext.CoinstakeTx.Time & ~PosConsensusOptions.StakeTimestampMask) < minimalAllowedTime)
            {
                this.logger.LogTrace("(-)[TOO_EARLY_TIME_AFTER_LAST_BLOCK]:false");
                return false;
            }

            // Create worker tasks that will look for kernel.
            // Run task in parallel using the default task scheduler.
            int coinIndex = 0;
            int workerCount = (stakingUtxoDescriptions.Count + UtxoStakeDescriptionsPerCoinstakeWorker - 1) /
                              UtxoStakeDescriptionsPerCoinstakeWorker;
            var workers = new Task[workerCount];
            var workerContexts = new CoinstakeWorkerContext[workerCount];

            var workersResult = new CoinstakeWorkerResult();
            for (int workerIndex = 0; workerIndex < workerCount; workerIndex++)
            {
                var cwc = new CoinstakeWorkerContext
                {
                    Index = workerIndex,
                    Logger = this.loggerFactory.CreateLogger(this.GetType().FullName, $"[Worker #{workerIndex}] "),
                    utxoStakeDescriptions = new List<UtxoStakeDescription>(),
                    CoinstakeContext = coinstakeContext,
                    Result = workersResult
                };

                int stakingUtxoCount = Math.Min(stakingUtxoDescriptions.Count - coinIndex,
                    UtxoStakeDescriptionsPerCoinstakeWorker);
                cwc.utxoStakeDescriptions.AddRange(stakingUtxoDescriptions.GetRange(coinIndex, stakingUtxoCount));
                coinIndex += stakingUtxoCount;
                workerContexts[workerIndex] = cwc;

                workers[workerIndex] = Task.Run(() =>
                    this.CoinstakeWorker(cwc, chainTip, block, minimalAllowedTime, searchInterval));
            }

            await Task.WhenAll(workers).ConfigureAwait(false);

            if (workersResult.KernelFoundIndex == CoinstakeWorkerResult.KernelNotFound)
            {
                this.logger.LogTrace("(-)[KERNEL_NOT_FOUND]:false");
                return false;
            }

            this.logger.LogTrace("Worker #{0} found the kernel.", workersResult.KernelFoundIndex);

            // Get reward for newly created block.
            long reward = fees + this.consensusManager.ConsensusRules.GetRule<PosCoinviewRule>()
                              .GetProofOfStakeReward(chainTip.Height + 1);
            if (reward <= 0)
            {
                // TODO: This can't happen unless we remove reward for mined block.
                // If this can happen over time then this check could be done much sooner
                // to avoid a lot of computation.
                this.logger.LogTrace("(-)[NO_REWARD]:false");
                return false;
            }

            // Input to coinstake transaction.
            UtxoStakeDescription coinstakeInput = workersResult.KernelCoin;

            int eventuallyStakableUtxosCount = utxoStakeDescriptions.Count;
            Transaction coinstakeTx = this.PrepareCoinStakeTransactions(chainTip.Height, coinstakeContext,
                coinstakeInput.TxOut.Value, fees, eventuallyStakableUtxosCount, ourWeight);

            // Sign.
            if (!this.SignTransactionInput(coinstakeInput, coinstakeTx))
            {
                this.logger.LogTrace("(-)[SIGN_FAILED]:false");
                return false;
            }

            // Limit size.
            int serializedSize =
                coinstakeContext.CoinstakeTx.GetSerializedSize(ProtocolVersion.ALT_PROTOCOL_VERSION,
                    SerializationType.Network);
            if (serializedSize >= (MaxBlockSizeGen / 5))
            {
                this.logger.LogTrace("Coinstake size {0} bytes exceeded limit {1} bytes.", serializedSize,
                    MaxBlockSizeGen / 5);
                this.logger.LogTrace("(-)[SIZE_EXCEEDED]:false");
                return false;
            }

            // Successfully generated coinstake.
            return true;
        }

        private Transaction PrepareCoinStakeTransactions(int currentChainHeight, CoinstakeContext coinstakeContext,long totalOut, long fees, int utxosCount, long amountStaked)
        {
            // Splits fees between miner and DeStream
            // Splits miner's output if needed
            // Output to DeStream stays in the end of array

            this.DeStreamNetwork.SplitFee(fees, out long deStreamFee, out long minerReward);

            // Split stake into SplitFactor utxos if above threshold.
            bool shouldSplitStake = this.ShouldSplitStake(utxosCount, amountStaked, totalOut, currentChainHeight);

            int lastOutputIndex = coinstakeContext.CoinstakeTx.Outputs.Count - 1;

            if (!shouldSplitStake)
            {
                coinstakeContext.CoinstakeTx.Outputs[lastOutputIndex - 1].Value = totalOut + minerReward;
                coinstakeContext.CoinstakeTx.Outputs[lastOutputIndex].Value = deStreamFee;

                this.logger.LogTrace(
                    "Coinstake miner output value is {0}, DeStream output value is {1}.",
                    coinstakeContext.CoinstakeTx.Outputs[lastOutputIndex - 1].Value,
                    coinstakeContext.CoinstakeTx.Outputs[lastOutputIndex].Value);
                this.logger.LogTrace("(-)[NO_SPLIT]:{0}", coinstakeContext.CoinstakeTx);

                return coinstakeContext.CoinstakeTx;
            }

            long splitValue = (totalOut + minerReward) / SplitFactor;
            long remainder = (totalOut + minerReward) - ((SplitFactor - 1) * splitValue);
            coinstakeContext.CoinstakeTx.Outputs[lastOutputIndex - 1].Value = remainder;

            for (int i = 0; i < SplitFactor - 1; i++)
            {
                var split = new TxOut(splitValue,
                    coinstakeContext.CoinstakeTx.Outputs[lastOutputIndex - 1].ScriptPubKey);
                coinstakeContext.CoinstakeTx.Outputs.Insert(coinstakeContext.CoinstakeTx.Outputs.Count - 1, split);
            }

            this.logger.LogTrace(
                "Coinstake output value has been split into {0} outputs of {1} and a remainder of {2}.",
                SplitFactor - 1, splitValue, remainder);

            return coinstakeContext.CoinstakeTx;
        }

        protected override bool SignTransactionInput(UtxoStakeDescription input, Transaction transaction)
        {
            // Creates DeStreamTransactionBuilder

            bool res = false;
            try
            {
                var transactionBuilder = new DeStreamTransactionBuilder(this.network)
                    .AddKeys(input.Key)
                    .AddCoins(new Coin(input.OutPoint, input.TxOut));

                foreach (BuilderExtension extension in this.walletManager.GetTransactionBuilderExtensionsForStaking())
                    transactionBuilder.Extensions.Add(extension);

                transactionBuilder.SignTransactionInPlace(transaction);

                res = true;
            }
            catch (Exception e)
            {
                this.logger.LogDebug("Exception occurred: {0}", e.ToString());
            }

            return res;
        }

        public DeStreamPosMinting(IBlockProvider blockProvider, IConsensusManager consensusManager,
            ConcurrentChain chain, Network network, IDateTimeProvider dateTimeProvider,
            IInitialBlockDownloadState initialBlockDownloadState, INodeLifetime nodeLifetime, ICoinView coinView,
            IStakeChain stakeChain, IStakeValidator stakeValidator, MempoolSchedulerLock mempoolLock,
            ITxMempool mempool, IWalletManager walletManager, IAsyncLoopFactory asyncLoopFactory,
            ITimeSyncBehaviorState timeSyncBehaviorState, ILoggerFactory loggerFactory, MinerSettings minerSettings) :
            base(blockProvider, consensusManager, chain, network, dateTimeProvider, initialBlockDownloadState,
                nodeLifetime, coinView, stakeChain, stakeValidator, mempoolLock, mempool, walletManager,
                asyncLoopFactory, timeSyncBehaviorState, loggerFactory, minerSettings)
        {
        }
    }
}
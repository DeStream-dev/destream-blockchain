using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NBitcoin;
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;
using Stratis.Bitcoin.Base;
using Stratis.Bitcoin.Connection;
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

namespace Stratis.Bitcoin.Features.Miner
{
    public class DeStreamPosMinting : PosMinting
    {
        public DeStreamPosMinting(IBlockProvider blockProvider, IConsensusLoop consensusLoop, ConcurrentChain chain,
            Network network, IConnectionManager connectionManager, IDateTimeProvider dateTimeProvider,
            IInitialBlockDownloadState initialBlockDownloadState, INodeLifetime nodeLifetime, CoinView coinView,
            IStakeChain stakeChain, IStakeValidator stakeValidator, MempoolSchedulerLock mempoolLock,
            ITxMempool mempool, IWalletManager walletManager, IAsyncLoopFactory asyncLoopFactory,
            ITimeSyncBehaviorState timeSyncBehaviorState, ILoggerFactory loggerFactory) : base(blockProvider,
            consensusLoop, chain, network, connectionManager, dateTimeProvider, initialBlockDownloadState, nodeLifetime,
            coinView, stakeChain, stakeValidator, mempoolLock, mempool, walletManager, asyncLoopFactory,
            timeSyncBehaviorState, loggerFactory)
        {
        }

        protected override void CoinstakeWorker(CoinstakeWorkerContext context, ChainedHeader chainTip, Block block,
            long minimalAllowedTime, long searchInterval)
        {
            base.CoinstakeWorker(context, chainTip, block, minimalAllowedTime, searchInterval);

            if (context.Result.KernelFoundIndex == CoinstakeWorkerResult.KernelNotFound)
                return;

            Script key = new KeyId(new uint160(Encoders.Base58Check.DecodeData(this.network.DeStreamWallets.First())
                .Skip(this.network.Base58Prefixes[(int) Base58Type.PUBKEY_ADDRESS].Length).ToArray())).ScriptPubKey;

            context.CoinstakeContext.CoinstakeTx.AddOutput(new TxOut(0, key));
        }

        // No way to call base function and change smth after this, replacing whole function is the only way.
        // All modified code is extraced to functions.
        public override async Task<bool> CreateCoinstakeAsync(List<UtxoStakeDescription> utxoStakeDescriptions,
            Block block, ChainedHeader chainTip, long searchInterval,
            long fees, CoinstakeContext coinstakeContext)
        {
            this.logger.LogTrace("({0}.{1}:{2},{3}:'{4}',{5}:{6},{7}:{8})", nameof(utxoStakeDescriptions),
                nameof(utxoStakeDescriptions.Count), utxoStakeDescriptions.Count, nameof(chainTip), chainTip,
                nameof(searchInterval), searchInterval, nameof(fees), fees);

            int nonEmptyUtxos = utxoStakeDescriptions.Count;
            coinstakeContext.CoinstakeTx.Inputs.Clear();
            coinstakeContext.CoinstakeTx.Outputs.Clear();

            // Mark coinstake transaction.
            coinstakeContext.CoinstakeTx.Outputs.Add(new TxOut(Money.Zero, new Script()));

            long balance = this.GetMatureBalance(utxoStakeDescriptions).Satoshi;
            if (balance <= this.targetReserveBalance)
            {
                this.rpcGetStakingInfoModel.Staking = false;

                this.logger.LogTrace(
                    "Total balance of available UTXOs is {0}, which is less than or equal to reserve balance {1}.",
                    balance, this.targetReserveBalance);
                this.logger.LogTrace("(-)[BELOW_RESERVE]:false");
                return false;
            }

            // Select UTXOs with suitable depth.
            List<UtxoStakeDescription> stakingUtxoDescriptions =
                this.GetUtxoStakeDescriptionsSuitableForStaking(utxoStakeDescriptions, chainTip,
                    coinstakeContext.CoinstakeTx.Time, balance - this.targetReserveBalance);
            if (!stakingUtxoDescriptions.Any())
            {
                this.rpcGetStakingInfoModel.Staking = false;
                this.logger.LogTrace("(-)[NO_SELECTION]:false");
                return false;
            }

            long ourWeight = stakingUtxoDescriptions.Sum(s => s.TxOut.Value);
            long expectedTime = StakeValidator.TargetSpacingSeconds * this.networkWeight / ourWeight;
            decimal ourPercent = this.networkWeight != 0 ? 100.0m * ourWeight / this.networkWeight : 0;

            this.logger.LogInformation(
                "Node staking with {0} ({1:0.00} % of the network weight {2}), est. time to find new block is {3}.",
                new Money(ourWeight), ourPercent, new Money(this.networkWeight), TimeSpan.FromSeconds(expectedTime));

            this.rpcGetStakingInfoModel.Staking = true;
            this.rpcGetStakingInfoModel.Weight = ourWeight;
            this.rpcGetStakingInfoModel.ExpectedTime = expectedTime;
            this.rpcGetStakingInfoModel.Errors = null;

            long minimalAllowedTime = chainTip.Header.Time + 1;
            this.logger.LogTrace(
                "Trying to find staking solution among {0} transactions, minimal allowed time is {1}, coinstake time is {2}.",
                stakingUtxoDescriptions.Count, minimalAllowedTime, coinstakeContext.CoinstakeTx.Time);

            // If the time after applying the mask is lower than minimal allowed time,
            // it is simply too early for us to mine, there can't be any valid solution.
            if ((coinstakeContext.CoinstakeTx.Time & ~BlockHeaderPosContextualRule.StakeTimestampMask) <
                minimalAllowedTime)
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
            long reward = this.GetReward(fees, chainTip.Height);
            if (reward < 0)
            {
                // TODO: This can't happen unless we remove reward for mined block.
                // If this can happen over time then this check could be done much sooner
                // to avoid a lot of computation.
                this.logger.LogTrace("(-)[NO_REWARD]:false");
                return false;
            }

            // Split stake if above threshold.
            this.SplitStake(nonEmptyUtxos, chainTip, coinstakeContext.CoinstakeTx.Outputs);

            // Input to coinstake transaction.
            UtxoStakeDescription coinstakeInput = workersResult.KernelCoin;

            // Total amount of input values in coinstake transaction.
            long coinstakeInputValue = coinstakeInput.TxOut.Value + reward;

            // Set output amount.
            this.SetOutputAmount(coinstakeContext.CoinstakeTx.Outputs, coinstakeInputValue, fees);

            // Sign.
            if (!this.SignTransactionInput(coinstakeInput, coinstakeContext.CoinstakeTx))
            {
                this.logger.LogTrace("(-)[SIGN_FAILED]:false");
                return false;
            }

            // Limit size.
            int serializedSize =
                coinstakeContext.CoinstakeTx.GetSerializedSize(ProtocolVersion.ALT_PROTOCOL_VERSION,
                    SerializationType.Network);
            if (serializedSize >= MaxBlockSizeGen / 5)
            {
                this.logger.LogTrace("Coinstake size {0} bytes exceeded limit {1} bytes.", serializedSize,
                    MaxBlockSizeGen / 5);
                this.logger.LogTrace("(-)[SIZE_EXCEEDED]:false");
                return false;
            }

            // Successfully generated coinstake.
            this.logger.LogTrace("(-):true");
            return true;
        }

        private void SetOutputAmount(TxOutList outputs, long coinstakeInputValue, long fees)
        {
            if (outputs.Count == 4)
            {
                outputs[1].Value = coinstakeInputValue / 2 / Money.CENT * Money.CENT;
                outputs[2].Value = coinstakeInputValue - outputs[1].Value;
                outputs[3].Value = (long) (fees * this.network.DeStreamFeePart);
                this.logger.LogTrace("Coinstake first output value is {0}, second is {1}, third is {3}.",
                    outputs[1].Value, outputs[2].Value, outputs[3].Value);
            }
            else
            {
                outputs[1].Value = coinstakeInputValue;
                outputs[2].Value = (long) (fees * this.network.DeStreamFeePart);
                this.logger.LogTrace("Coinstake first output value is {0}, second is {1} .", outputs[1].Value,
                    outputs[2].Value);
            }
        }

        private long GetReward(long fees, int chainTipHeight)
        {
            return (long) (fees * (1 - this.network.DeStreamFeePart)) +
                   this.consensusLoop.ConsensusRules.GetRule<PosCoinviewRule>()
                       .GetProofOfStakeReward(chainTipHeight + 1);
        }

        private void SplitStake(int nonEmptyUtxos, ChainedHeader chainTip, TxOutList outputs)
        {
            if (!this.GetSplitStake(nonEmptyUtxos, chainTip)) return;

            this.logger.LogTrace("Coinstake UTXO will be split to two.");
            outputs.Insert(2, new TxOut(0, outputs[1].ScriptPubKey));
        }
    }
}
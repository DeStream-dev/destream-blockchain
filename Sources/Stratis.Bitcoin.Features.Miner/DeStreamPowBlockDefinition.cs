using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using NBitcoin;
using NBitcoin.DataEncoders;
using Stratis.Bitcoin.Consensus.Rules;
using Stratis.Bitcoin.Features.Consensus.Interfaces;
using Stratis.Bitcoin.Features.Consensus.Rules.CommonRules;
using Stratis.Bitcoin.Features.MemoryPool;
using Stratis.Bitcoin.Features.MemoryPool.Interfaces;
using Stratis.Bitcoin.Utilities;

namespace Stratis.Bitcoin.Features.Miner
{
    public class DeStreamPowBlockDefinition : PowBlockDefinition
    {
        public DeStreamPowBlockDefinition(IConsensusLoop consensusLoop, IDateTimeProvider dateTimeProvider, ILoggerFactory loggerFactory, ITxMempool mempool, MempoolSchedulerLock mempoolLock, Network network, IConsensusRules consensusRules, BlockDefinitionOptions options = null) : base(consensusLoop, dateTimeProvider, loggerFactory, mempool, mempoolLock, network, consensusRules, options)
        {
        }

        protected override void OnBuild(ChainedHeader chainTip, Script scriptPubKey)
        {
            this.Configure();

            this.ChainTip = chainTip;

            this.block = this.BlockTemplate.Block;
            this.scriptPubKey = scriptPubKey;

            this.CreateCoinbase();
            this.ComputeBlockVersion();

            // TODO: MineBlocksOnDemand
            // -regtest only: allow overriding block.nVersion with
            // -blockversion=N to test forking scenarios
            //if (this.network. chainparams.MineBlocksOnDemand())
            //    pblock->nVersion = GetArg("-blockversion", pblock->nVersion);

            this.MedianTimePast = Utils.DateTimeToUnixTime(this.ChainTip.GetMedianTimePast());
            this.LockTimeCutoff =
                MempoolValidator.StandardLocktimeVerifyFlags.HasFlag(Transaction.LockTimeFlags.MedianTimePast)
                    ? this.MedianTimePast
                    : this.block.Header.Time;

            // TODO: Implement Witness Code
            // Decide whether to include witness transactions
            // This is only needed in case the witness softfork activation is reverted
            // (which would require a very deep reorganization) or when
            // -promiscuousmempoolflags is used.
            // TODO: replace this with a call to main to assess validity of a mempool
            // transaction (which in most cases can be a no-op).
            this.IncludeWitness = false; //IsWitnessEnabled(pindexPrev, chainparams.GetConsensus()) && fMineWitnessTx;

            // add transactions from the mempool
            int nPackagesSelected;
            int nDescendantsUpdated;
            this.AddTransactions(out nPackagesSelected, out nDescendantsUpdated);

            this.LastBlockTx = this.BlockTx;
            this.LastBlockSize = this.BlockSize;
            this.LastBlockWeight = this.BlockWeight;

            // TODO: Implement Witness Code
            // pblocktemplate->CoinbaseCommitment = GenerateCoinbaseCommitment(*pblock, pindexPrev, chainparams.GetConsensus());

            var coinviewRule = this.ConsensusLoop.ConsensusRules.GetRule<CoinViewRule>();
            this.Network.SplitFee(this.fees.Satoshi, out long deStreamFee, out long minerReward);
            this.coinbase.Outputs[0].Value =  minerReward;
            this.coinbase.Outputs[1].Value = deStreamFee;
            this.BlockTemplate.TotalFee = this.fees;

            int nSerializeSize = this.block.GetSerializedSize();
            this.logger.LogDebug(
                "Serialized size is {0} bytes, block weight is {1}, number of txs is {2}, tx fees are {3}, number of sigops is {4}.",
                nSerializeSize, coinviewRule.GetBlockWeight(this.block), this.BlockTx, this.fees, this.BlockSigOpsCost);

            this.UpdateHeaders();
        }

        protected override void CreateCoinbase()
        {
            base.CreateCoinbase();

            Script deStreamAddressKey = new KeyId(new uint160(Encoders.Base58Check
                .DecodeData(this.Network.DeStreamWallet)
                .Skip(this.Network.Base58Prefixes[(int) Base58Type.PUBKEY_ADDRESS].Length).ToArray())).ScriptPubKey;
            this.coinbase.AddOutput(new TxOut(Money.Zero, deStreamAddressKey));
        }
    }
}
using Microsoft.Extensions.Logging;
using NBitcoin;
using Stratis.Bitcoin.Base.Deployments;
using Stratis.Bitcoin.BlockPulling;
using Stratis.Bitcoin.Configuration.Settings;
using Stratis.Bitcoin.Consensus;
using Stratis.Bitcoin.Consensus.Rules;
using Stratis.Bitcoin.Features.Consensus.CoinViews;
using Stratis.Bitcoin.Features.Consensus.Interfaces;
using Stratis.Bitcoin.Utilities;

namespace Stratis.Bitcoin.Features.Consensus.Rules
{
    public class DeStreamPowConsensusRules : PowConsensusRules
    {
        public DeStreamPowConsensusRules(Network network, ILoggerFactory loggerFactory,
            IDateTimeProvider dateTimeProvider, ConcurrentChain chain, NodeDeployments nodeDeployments,
            ConsensusSettings consensusSettings, ICheckpoints checkpoints, CoinView utxoSet,
            ILookaheadBlockPuller puller) : base(network, loggerFactory, dateTimeProvider, chain, nodeDeployments,
            consensusSettings, checkpoints, utxoSet, puller)
        {
        }

        public override RuleContext CreateRuleContext(ValidationContext validationContext, ChainedHeader consensusTip)
        {
            return new DeStreamPowRuleContext(validationContext, this.Network.Consensus, consensusTip);
        }
    }

    public class DeStreamPosConsensusRules : PosConsensusRules
    {
        public DeStreamPosConsensusRules(Network network, ILoggerFactory loggerFactory,
            IDateTimeProvider dateTimeProvider, ConcurrentChain chain, NodeDeployments nodeDeployments,
            ConsensusSettings consensusSettings, ICheckpoints checkpoints, CoinView utxoSet,
            ILookaheadBlockPuller puller, IStakeChain stakeChain, IStakeValidator stakeValidator) : base(network,
            loggerFactory, dateTimeProvider, chain, nodeDeployments, consensusSettings, checkpoints, utxoSet, puller,
            stakeChain, stakeValidator)
        {
        }

        public override RuleContext CreateRuleContext(ValidationContext validationContext, ChainedHeader consensusTip)
        {
            return new DeStreamRuleContext(validationContext, this.Network.Consensus, consensusTip);
        }
    }
}
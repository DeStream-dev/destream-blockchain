using Microsoft.Extensions.Logging;
using NBitcoin;
using Stratis.Bitcoin.Base;
using Stratis.Bitcoin.Base.Deployments;
using Stratis.Bitcoin.Configuration.Settings;
using Stratis.Bitcoin.Consensus;
using Stratis.Bitcoin.Consensus.Rules;
using Stratis.Bitcoin.Features.Consensus.CoinViews;
using Stratis.Bitcoin.Features.Consensus.Interfaces;
using Stratis.Bitcoin.Features.Consensus.ProvenBlockHeaders;
using Stratis.Bitcoin.Utilities;

namespace Stratis.Bitcoin.Features.Consensus.Rules
{
    public class DeStreamPowConsensusRuleEngine : PowConsensusRuleEngine
    {
        public DeStreamPowConsensusRuleEngine(Network network, ILoggerFactory loggerFactory,
            IDateTimeProvider dateTimeProvider, ConcurrentChain chain, NodeDeployments nodeDeployments,
            ConsensusSettings consensusSettings, ICheckpoints checkpoints, ICoinView utxoSet, IChainState chainState,
            IInvalidBlockHashStore invalidBlockHashStore, INodeStats nodeStats) : base(network, loggerFactory,
            dateTimeProvider, chain, nodeDeployments, consensusSettings, checkpoints, utxoSet, chainState,
            invalidBlockHashStore, nodeStats)
        {
        }

        public override RuleContext CreateRuleContext(ValidationContext validationContext)
        {
            return new DeStreamPowRuleContext(validationContext, DateTimeProvider.GetTimeOffset());
        }
    }

    public class DeStreamPosConsensusRuleEngine : PosConsensusRuleEngine
    {
        public DeStreamPosConsensusRuleEngine(Network network, ILoggerFactory loggerFactory,
            IDateTimeProvider dateTimeProvider, ConcurrentChain chain, NodeDeployments nodeDeployments,
            ConsensusSettings consensusSettings, ICheckpoints checkpoints, ICoinView utxoSet, IStakeChain stakeChain,
            IStakeValidator stakeValidator, IChainState chainState, IInvalidBlockHashStore invalidBlockHashStore,
            INodeStats nodeStats, IRewindDataIndexCache rewindDataIndexCache) : base(network, loggerFactory,
            dateTimeProvider, chain, nodeDeployments, consensusSettings, checkpoints, utxoSet, stakeChain,
            stakeValidator, chainState, invalidBlockHashStore, nodeStats, rewindDataIndexCache)
        {
        }

        public override RuleContext CreateRuleContext(ValidationContext validationContext)
        {
            return new DeStreamPosRuleContext(validationContext, DateTimeProvider.GetTimeOffset());
        }
    }
}
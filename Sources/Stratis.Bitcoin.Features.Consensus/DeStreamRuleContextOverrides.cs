using System.Collections.Generic;
using NBitcoin;
using Stratis.Bitcoin.Consensus;

namespace Stratis.Bitcoin.Features.Consensus
{
    /// <inheritdoc />
    public class DeStreamPosRuleContext : PosRuleContext
    {
        internal DeStreamPosRuleContext()
        {
        }

        public DeStreamPosRuleContext(ValidationContext validationContext, NBitcoin.Consensus consensus,
            ChainedHeader consensusTip) : base(validationContext, consensus, consensusTip)
        {
        }

        public List<Script> InputScriptPubKeys { get; set; }

        public Money TotalIn { get; set; }
    }

    /// <inheritdoc />
    public class DeStreamPowRuleContext : PowRuleContext
    {
        internal DeStreamPowRuleContext()
        {
        }

        public DeStreamPowRuleContext(ValidationContext validationContext, NBitcoin.Consensus consensus,
            ChainedHeader consensusTip) : base(validationContext, consensus, consensusTip)
        {
        }

        public List<Script> InputScriptPubKeys { get; set; }

        public Money TotalIn { get; set; }
    }
}
using System.Collections.Generic;
using NBitcoin;
using Stratis.Bitcoin.Consensus;

namespace Stratis.Bitcoin.Features.Consensus
{
    public interface IDeStreamRuleContext
    {
        /// <summary>
        ///     ScriptPubKeys of inputs spent in transaction
        /// </summary>
        List<Script> InputScriptPubKeys { get; set; }

        /// <summary>
        ///     Sum of inputs spent in transaction
        /// </summary>
        IDictionary<uint256, Money> TotalIn { get; set; }
    }

    /// <inheritdoc cref="PosRuleContext" />
    public class DeStreamRuleContext : PosRuleContext, IDeStreamRuleContext
    {
        internal DeStreamRuleContext()
        {
        }

        public DeStreamRuleContext(ValidationContext validationContext, NBitcoin.Consensus consensus,
            ChainedHeader consensusTip) : base(validationContext, consensus, consensusTip)
        {
        }

        /// <inheritdoc />
        public List<Script> InputScriptPubKeys { get; set; }


        /// <inheritdoc />
        public IDictionary<uint256, Money> TotalIn { get; set; } = new Dictionary<uint256, Money>();
    }

    /// <inheritdoc cref="PowRuleContext" />
    public class DeStreamPowRuleContext : PowRuleContext, IDeStreamRuleContext
    {
        internal DeStreamPowRuleContext()
        {
        }

        public DeStreamPowRuleContext(ValidationContext validationContext, NBitcoin.Consensus consensus,
            ChainedHeader consensusTip) : base(validationContext, consensus, consensusTip)
        {
        }

        /// <inheritdoc />
        public List<Script> InputScriptPubKeys { get; set; }

        /// <inheritdoc />
        public IDictionary<uint256, Money> TotalIn { get; set; } = new Dictionary<uint256, Money>();
    }
}
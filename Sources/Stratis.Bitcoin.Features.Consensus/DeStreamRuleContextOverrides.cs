using System;
using System.Collections.Generic;
using NBitcoin;
using Stratis.Bitcoin.Consensus;

namespace Stratis.Bitcoin.Features.Consensus
{
    public interface IDeStreamRuleContext
    {
        /// <summary>
        ///     Gets or sets scriptPubKeys of inputs spent in transaction
        /// </summary>
        IDictionary<uint256, List<Script>> InputScriptPubKeys { get; set; }

        /// <summary>
        ///     Gets or sets sum of inputs spent in transaction
        /// </summary>
        IDictionary<uint256, Money> TotalIn { get; set; }
    }

    /// <inheritdoc cref="PosRuleContext" />
    public class DeStreamPosRuleContext : PosRuleContext, IDeStreamRuleContext
    {
        internal DeStreamPosRuleContext()
        {
        }

        public DeStreamPosRuleContext(BlockStake blockStake)
            : base(blockStake)
        {
        }

        public DeStreamPosRuleContext(ValidationContext validationContext, DateTimeOffset time)
            : base(validationContext,time)
        {
        }

        /// <inheritdoc />
        public IDictionary<uint256, List<Script>> InputScriptPubKeys { get; set; }


        /// <inheritdoc />
        public IDictionary<uint256, Money> TotalIn { get; set; } = new Dictionary<uint256, Money>();
    }

    /// <inheritdoc cref="PowRuleContext" />
    public class DeStreamPowRuleContext : PowRuleContext, IDeStreamRuleContext
    {
        internal DeStreamPowRuleContext()
        {
        }

        public DeStreamPowRuleContext(ValidationContext validationContext, DateTimeOffset time)
            : base(validationContext, time)
        {
        }

        /// <inheritdoc />
        public IDictionary<uint256, List<Script>> InputScriptPubKeys { get; set; }

        /// <inheritdoc />
        public IDictionary<uint256, Money> TotalIn { get; set; } = new Dictionary<uint256, Money>();
    }
}
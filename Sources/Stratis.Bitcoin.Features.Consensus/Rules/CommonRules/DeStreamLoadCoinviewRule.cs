using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Stratis.Bitcoin.Consensus;
using Stratis.Bitcoin.Consensus.Rules;
using Stratis.Bitcoin.Features.Consensus.CoinViews;
using Stratis.Bitcoin.Utilities;

namespace Stratis.Bitcoin.Features.Consensus.Rules.CommonRules
{
    public class DeStreamLoadCoinviewRule : LoadCoinviewRule
    {
        public override async Task RunAsync(RuleContext context)
        {
            // Check that the current block has not been reorged.
            // Catching a reorg at this point will not require a rewind.
            if (context.ValidationContext.BlockToValidate.Header.HashPrevBlock != this.Parent.ChainState.ConsensusTip.HashBlock)
            {
                this.Logger.LogTrace("Reorganization detected.");
                ConsensusErrors.InvalidPrevTip.Throw();
            }

            var utxoRuleContext = context as UtxoRuleContext;
            
            switch (utxoRuleContext)
            {
                case DeStreamPowRuleContext deStreamPowRuleContext:
                    deStreamPowRuleContext.InputScriptPubKeys = new Dictionary<uint256, List<Script>>();
                    break;
                case DeStreamPosRuleContext deStreamPosRuleContext:
                    deStreamPosRuleContext.InputScriptPubKeys = new Dictionary<uint256, List<Script>>();
                    break;
                default:
                    throw new NotSupportedException(
                        $"Rule context must be {nameof(DeStreamPowRuleContext)} or {nameof(DeStreamPosRuleContext)}");
            }

            // Load the UTXO set of the current block. UTXO may be loaded from cache or from disk.
            // The UTXO set is stored in the context.
            this.Logger.LogTrace("Loading UTXO set of the new block.");
            utxoRuleContext.UnspentOutputSet = new DeStreamUnspentOutputSet();

            uint256[] ids = this.coinviewHelper.GetIdsToFetch(context.ValidationContext.BlockToValidate, context.Flags.EnforceBIP30);
            FetchCoinsResponse coins = await this.PowParent.UtxoSet.FetchCoinsAsync(ids).ConfigureAwait(false);
            utxoRuleContext.UnspentOutputSet.SetCoins(coins.UnspentOutputs);
        }
    }
}
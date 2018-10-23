﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Stratis.Bitcoin.Consensus.Rules;
using Stratis.Bitcoin.Features.Consensus.CoinViews;
using Stratis.Bitcoin.Utilities;

namespace Stratis.Bitcoin.Features.Consensus.Rules.CommonRules
{
    [ExecutionRule]
    public class DeStreamLoadCoinviewRule : LoadCoinviewRule
    {
        public override async Task RunAsync(RuleContext context)
        {
            var utxoRuleContext = context as UtxoRuleContext;

            // Load the UTXO set of the current block. UTXO may be loaded from cache or from disk.
            // The UTXO set is stored in the context.
            this.Logger.LogTrace("Loading UTXO set of the new block.");
            utxoRuleContext.UnspentOutputSet = new DeStreamUnspentOutputSet();

            switch (utxoRuleContext)
            {
                case DeStreamPowRuleContext deStreamPowRuleContext:
                    deStreamPowRuleContext.InputScriptPubKeys = new List<Script>();
                    break;
                case DeStreamRuleContext deStreamPosRuleContext:
                    deStreamPosRuleContext.InputScriptPubKeys = new List<Script>();
                    break;
                default:
                    throw new NotSupportedException(
                        $"Rule context must be {nameof(DeStreamPowRuleContext)} or {nameof(DeStreamRuleContext)}");
            }

            using (new StopwatchDisposable(o => this.Parent.PerformanceCounter.AddUTXOFetchingTime(o)))
            {
                uint256[] ids = this.GetIdsToFetch(context.ValidationContext.Block, context.Flags.EnforceBIP30);
                FetchCoinsResponse coins = await this.PowParent.UtxoSet.FetchCoinsAsync(ids).ConfigureAwait(false);
                utxoRuleContext.UnspentOutputSet.SetCoins(coins.UnspentOutputs);
            }

            // Attempt to load into the cache the next set of UTXO to be validated.
            // The task is not awaited so will not stall main validation process.
            this.TryPrefetchAsync(context.Flags);
        }
    }
}
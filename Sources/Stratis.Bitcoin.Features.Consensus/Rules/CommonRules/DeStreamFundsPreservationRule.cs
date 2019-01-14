using System;
using System.Linq;
using System.Threading.Tasks;
using NBitcoin;
using Stratis.Bitcoin.Consensus;
using Stratis.Bitcoin.Consensus.Rules;

namespace Stratis.Bitcoin.Features.Consensus.Rules.CommonRules
{
    /// <summary>
    ///     A rule verifies that total amount of coins in blockchain is not changed
    /// </summary>
    public class DeStreamFundsPreservationRule : UtxoStoreConsensusRule
    {
        public override Task RunAsync(RuleContext context)
        {
            UnspentOutputSet inputs =
                (context as UtxoRuleContext ??
                 throw new NotSupportedException($"Rule context must be {nameof(UtxoRuleContext)}")).UnspentOutputSet;
            long totalIn = context.ValidationContext.BlockToValidate.Transactions.SelectMany(p =>
                    p.Inputs.RemoveChangePointer().Where(q => q.PrevOut != null)
                        .Select(q => inputs.AccessCoins(q.PrevOut.Hash)?.TryGetOutput(q.PrevOut.N)?.Value ?? 0))
                .Sum(p => p ?? 0);

            long totalOut = context.ValidationContext.BlockToValidate.Transactions.SelectMany(p => p.Outputs.Select(q => q.Value))
                .Sum(p => p);

            if (totalIn != totalOut)
                ConsensusErrors.BadBlockTotalFundsChanged.Throw();

            return Task.CompletedTask;
        }
    }
}
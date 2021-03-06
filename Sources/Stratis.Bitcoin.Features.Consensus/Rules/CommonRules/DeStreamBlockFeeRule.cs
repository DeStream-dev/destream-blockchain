using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NBitcoin;
using Stratis.Bitcoin.Consensus.Rules;

namespace Stratis.Bitcoin.Features.Consensus.Rules.CommonRules
{
    /// <summary>
    /// A rule that verifies fee is charged from all spent funds and transferred to <see cref="Network.DeStreamWallet"/>
    /// </summary>
    [ExecutionRule]
    public class DeStreamBlockFeeRule : ConsensusRule
    {
        public override Task RunAsync(RuleContext context)
        {
            // Actual fee is funds that are transferred to fee wallet in mined/staked block
            long actualFee = this.GetActualFee(context.ValidationContext.Block);
            
            // Expected fee is charged from all moved funds (not change)
            long expectedFee;
            switch (context)
            {
                case DeStreamPowRuleContext deStreamPowRuleContext:
                    expectedFee = this.GetExpectedFee(context.ValidationContext.Block, deStreamPowRuleContext.TotalIn,
                        deStreamPowRuleContext.InputScriptPubKeys);
                    break;
                case DeStreamRuleContext deStreamPosRuleContext:
                    expectedFee = this.GetExpectedFee(context.ValidationContext.Block, deStreamPosRuleContext.TotalIn,
                        deStreamPosRuleContext.InputScriptPubKeys);
                    break;
                default:
                    throw new NotSupportedException(
                        $"Rule context must be {nameof(DeStreamPowRuleContext)} or {nameof(DeStreamRuleContext)}");
            }

            this.Parent.Network.SplitFee(expectedFee, out long expectedDeStreamFee, out long _);
            
            if (actualFee < expectedDeStreamFee)
                ConsensusErrors.BadTransactionFeeOutOfRange.Throw();

            return Task.CompletedTask;
        }

        private long GetActualFee(Block block)
        {
            IList<TxOut> outputsToFeeWallet = block.Transactions[BlockStake.IsProofOfStake(block) ? 1 : 0].Outputs
                .Where(p => this.Parent.Network.IsDeStreamAddress(p.ScriptPubKey
                    .GetDestinationAddress(this.Parent.Network)?.ToString())).ToList();

            if (outputsToFeeWallet.Count != 1)
                ConsensusErrors.BadBlockNoFeeOutput.Throw();

            return outputsToFeeWallet.Single().Value;
        }

        private long GetExpectedFee(Block block, IDictionary<uint256, Money> totalIn, IDictionary<uint256, List<Script>> inputScriptPubKeys)
        {
            return block.Transactions.Where(p => !p.IsCoinBase && !p.IsCoinStake).Sum(p => this.GetFeeInTransaction(p,
                totalIn[p.GetHash()], p.Outputs
                    .Select(q => q.ScriptPubKey).Intersect(inputScriptPubKeys[p.GetHash()])
                    .Concat(p.Inputs.GetChangePointers()
                        .Select(q => p.Outputs[q].ScriptPubKey))
                    .Distinct()
                    .ToList()));
        }

        private long GetFeeInTransaction(Transaction transaction, Money totalIn,
            IEnumerable<Script> changeScriptPubKeys)
        {
            long feeInTransaction = Convert.ToInt64(transaction.Outputs
                                          .Where(p => !changeScriptPubKeys.Contains(p.ScriptPubKey))
                                          .Sum(p => p.Value) * this.Parent.Network.FeeRate);
            if (Math.Abs(totalIn.Satoshi - transaction.TotalOut.Satoshi - feeInTransaction) > 1)
                ConsensusErrors.BadTransactionFeeOutOfRange.Throw();

            return feeInTransaction;
        }
    }
}
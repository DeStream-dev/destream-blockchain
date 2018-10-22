using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NBitcoin;
using Stratis.Bitcoin.Consensus.Rules;

namespace Stratis.Bitcoin.Features.Consensus.Rules.CommonRules
{
    [ExecutionRule]
    public class DeStreamBlockFeeRule : ConsensusRule
    {
        public override Task RunAsync(RuleContext context)
        {
            long actualFee = this.GetActualFee(context.ValidationContext.Block);
            long expectedFee;
            switch (context)
            {
                case DeStreamPowRuleContext deStreamPowRuleContext:
                    expectedFee = this.GetExpectedFee(context.ValidationContext.Block, deStreamPowRuleContext.TotalIn,
                        deStreamPowRuleContext.InputScriptPubKeys);
                    break;
                case DeStreamPosRuleContext deStreamPosRuleContext:
                    expectedFee = this.GetExpectedFee(context.ValidationContext.Block, deStreamPosRuleContext.TotalIn,
                        deStreamPosRuleContext.InputScriptPubKeys);
                    break;
                default:
                    throw new Exception();
            }

            if (Math.Abs(actualFee - expectedFee) > Money.CENT)
                throw new Exception();

            return Task.CompletedTask;
        }

        private long GetActualFee(Block block)
        {
            IList<TxOut> outputsToFeeWallet = block.Transactions[BlockStake.IsProofOfStake(block) ? 1 : 0].Outputs
                .Where(p =>
                    this.Parent.Network.DeStreamWallets.Any(q =>
                        q == p.ScriptPubKey.GetDestinationAddress(this.Parent.Network)?.ToString())).ToList();

            if (outputsToFeeWallet.Count() != 1)
                throw new Exception();

            return outputsToFeeWallet.Single().Value;
        }

        private long GetExpectedFee(Block block, Money totalIn, ICollection<Script> inputScriptPubKeys)
        {
            long totalFee = 0;
            foreach (Transaction transaction in block.Transactions.Where(p => !p.IsCoinBase && !p.IsCoinStake))
            {
                IList<Script> changeScriptPubKeys = transaction.Outputs
                    .Select(q =>
                        q.ScriptPubKey)
                    .Intersect(inputScriptPubKeys)
                    .Concat(transaction.Inputs.GetChangePointers()
                        .Select(p => transaction.Outputs[p].ScriptPubKey))
                    .Distinct()
                    .ToList();

                long feeInTransaction = this.GetFeeInTransaction(transaction, totalIn, changeScriptPubKeys);

                totalFee += feeInTransaction;
            }

            return totalFee;
        }

        private long GetFeeInTransaction(Transaction transaction, Money totalIn,
            IEnumerable<Script> changeScriptPubKeys)
        {
            double feeInTransaction = transaction.Outputs
                                          .Where(p => !changeScriptPubKeys.Contains(p.ScriptPubKey))
                                          .Sum(p => p.Value) * this.Parent.Network.FeeRate;
            if (Math.Abs(totalIn.Satoshi - transaction.TotalOut.Satoshi - feeInTransaction) > Money.CENT)
                throw new Exception();

            return (long) (feeInTransaction * this.Parent.Network.DeStreamFeePart);
        }
    }
}
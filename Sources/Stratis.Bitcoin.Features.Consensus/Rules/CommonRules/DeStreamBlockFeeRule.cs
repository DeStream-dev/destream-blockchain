using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NBitcoin;
using Stratis.Bitcoin.Consensus;
using Stratis.Bitcoin.Consensus.Rules;

namespace Stratis.Bitcoin.Features.Consensus.Rules.CommonRules
{
    /// <summary>
    /// A rule that verifies fee is charged from all spent funds and transferred to <see cref="Network.DeStreamWallet"/>
    /// </summary>
    public class DeStreamBlockFeeRule : FullValidationConsensusRule
    {
        private DeStreamNetwork DeStreamNetwork
        {
            get
            {
                if (!(this.Parent.Network is DeStreamNetwork))
                    throw new NotSupportedException($"Network must be {nameof(NBitcoin.DeStreamNetwork)}");
                return (DeStreamNetwork) this.Parent.Network;
            }
        }

        public override Task RunAsync(RuleContext context)
        {
            // Actual fee is funds that are transferred to fee wallet in mined/staked block
            long actualFee = this.GetActualFee(context.ValidationContext.BlockToValidate);
            
            // Expected fee is charged from all moved funds (not change)
            long expectedFee;
            switch (context)
            {
                case DeStreamPowRuleContext deStreamPowRuleContext:
                    expectedFee = this.GetExpectedFee(context.ValidationContext.BlockToValidate, deStreamPowRuleContext.TotalIn,
                        deStreamPowRuleContext.InputScriptPubKeys);
                    break;
                case DeStreamPosRuleContext deStreamPosRuleContext:
                    expectedFee = this.GetExpectedFee(context.ValidationContext.BlockToValidate, deStreamPosRuleContext.TotalIn,
                        deStreamPosRuleContext.InputScriptPubKeys);
                    break;
                default:
                    throw new NotSupportedException(
                        $"Rule context must be {nameof(DeStreamPowRuleContext)} or {nameof(DeStreamPosRuleContext)}");
            }

            this.DeStreamNetwork.SplitFee(expectedFee, out long expectedDeStreamFee, out long _);
            
            if (actualFee < expectedDeStreamFee)
                ConsensusErrors.BadTransactionFeeOutOfRange.Throw();

            return Task.CompletedTask;
        }

        private long GetActualFee(Block block)
        {
            IList<TxOut> outputsToFeeWallet = block.Transactions[BlockStake.IsProofOfStake(block) ? 1 : 0].Outputs
                .Where(p => this.DeStreamNetwork.IsDeStreamAddress(p.ScriptPubKey
                    .GetDestinationAddress(this.DeStreamNetwork)?.ToString())).ToList();

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
                                          .Sum(p => p.Value) * this.DeStreamNetwork.FeeRate);
            if (Math.Abs(totalIn.Satoshi - transaction.TotalOut.Satoshi - feeInTransaction) > 1)
                ConsensusErrors.BadTransactionFeeOutOfRange.Throw();

            return feeInTransaction;
        }
    }
}
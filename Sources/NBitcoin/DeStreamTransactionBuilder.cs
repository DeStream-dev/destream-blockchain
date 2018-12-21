using System;
using System.Collections.Generic;
using System.Linq;
using NBitcoin.Policy;

namespace NBitcoin
{
    public class DeStreamTransactionBuilder : TransactionBuilder
    {
        public DeStreamTransactionBuilder(Network network) : base(network)
        {
            this.CoinSelector = new DeStreamCoinSelector();
        }

        public DeStreamTransactionBuilder(int seed, Network network) : base(seed, network)
        {
            this.CoinSelector = new DeStreamCoinSelector(seed);
        }

        protected override IEnumerable<ICoin> BuildTransaction(TransactionBuildingContext ctx, BuilderGroup group,
            IEnumerable<Func<TransactionBuildingContext, IMoney>> builders,
            IEnumerable<ICoin> coins, IMoney zero)
        {
            IEnumerable<ICoin> result = base.BuildTransaction(ctx, group, builders, coins, zero);

            // To secure that fee is charged from spending coins and not from change,
            // we add input with uint256.Zero hash that points to output with change
            int changeIndex = ctx.Transaction.Outputs.FindIndex(p =>
                p.ScriptPubKey == group.ChangeScript[(int) ctx.ChangeType]);

            if (changeIndex == -1) return result;

            if (ctx.Transaction.Inputs.Any(p => p.PrevOut.Hash == uint256.Zero && p.PrevOut.N == changeIndex))
                return result;

            var outPoint = new OutPoint
            {
                Hash = uint256.Zero,
                N = (uint) changeIndex
            };

            ctx.Transaction.AddInput(new TxIn
            {
                PrevOut = outPoint
            });

            group.Coins.TryAdd(outPoint, new Coin(uint256.Zero, outPoint.N,
                Money.Zero, group.ChangeScript[(int) ctx.ChangeType]));

            return result;
        }

        /// <inheritdoc />
        public override TransactionBuilder CoverTheRest()
        {
            if (this._CompletedTransaction == null)
                throw new InvalidOperationException(
                    "A partially built transaction should be specified by calling ContinueToBuild");

            Money spent = this._CompletedTransaction.Inputs.AsIndexedInputs().RemoveChangePointer().Select(txin =>
                {
                    ICoin c = this.FindCoin(txin.PrevOut);
                    if (c == null)
                        throw this.CoinNotFound(txin);
                    return c as Coin;
                })
                .Where(c => c != null)
                .Select(c => c.Amount)
                .Sum();

            Money toComplete = this._CompletedTransaction.TotalOut - spent;
            this.CurrentGroup.Builders.Add(ctx => toComplete < Money.Zero ? Money.Zero : toComplete);
            return this;
        }

        /// <inheritdoc />
        public override bool Verify(Transaction tx, Money expectedFees, out TransactionPolicyError[] errors)
        {
            if (tx == null)
                throw new ArgumentNullException(nameof(tx));
            
            ICoin[] coins = tx.Inputs.Select(i => this.FindCoin(i.PrevOut)).Where(c => c != null).ToArray();
            
            var exceptions = new List<TransactionPolicyError>();
            
            TransactionPolicyError[] policyErrors = DeStreamMinerTransactionPolicy.Instance.Check(tx, coins);
            exceptions.AddRange(policyErrors);
            
            policyErrors = this.StandardTransactionPolicy.Check(tx, coins);
            exceptions.AddRange(policyErrors);
            
            if (expectedFees != null)
            {
                Money fees = tx.GetFee(coins);
                if (fees != null)
                {
                    Money margin = Money.Zero;
                    if (this.DustPrevention)
                        margin = this.GetDust() * 2;
                    if (!fees.Almost(expectedFees, margin))
                    {
                        exceptions.Add(new NotEnoughFundsPolicyError("Fees different than expected",
                            expectedFees - fees));
                    }
                }
            }

            errors = exceptions.ToArray();
            return errors.Length == 0;
        }
    }
}
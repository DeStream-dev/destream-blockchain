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

            if (ctx.Transaction.Inputs.Any(p => p.PrevOut.Hash == uint256.Zero))
                return result;

            // To secure that fee is charged from spending coins and not from change,
            // we add input with uint256.Zero hash that points to output with change
            int changeIndex = ctx.Transaction.Outputs.FindIndex(p =>
                p.ScriptPubKey == group.ChangeScript[(int) ctx.ChangeType]);
            
            if (changeIndex == -1) return result;

            var outPoint = new OutPoint
            {
                Hash = uint256.Zero,
                N = (uint) changeIndex
            };

            ctx.Transaction.AddInput(new TxIn
            {
                PrevOut = outPoint
            });

            group.Coins.Add(outPoint, new Coin(uint256.Zero, outPoint.N,
                Money.Zero, @group.ChangeScript[(int) ctx.ChangeType]));

            return result;
        }
    }
}
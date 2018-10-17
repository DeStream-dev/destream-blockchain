using System.Linq;
using NBitcoin;
using Stratis.Bitcoin.Utilities;

namespace Stratis.Bitcoin.Features.Consensus
{
    public class DeStreamUnspentOutputSet : UnspentOutputSet
    {
        public override bool HaveInputs(Transaction tx)
        {
            return tx.Inputs.RemoveChangePointer().All(txin => this.GetOutputFor(txin) != null);
        }

        public override void Update(Transaction transaction, int height)
        {
            if (!transaction.IsCoinBase)
            {
                foreach (TxIn input in transaction.Inputs.RemoveChangePointer())
                {
                    UnspentOutputs c = this.AccessCoins(input.PrevOut.Hash);
                    c.Spend(input.PrevOut.N);
                }
            }

            this.unspents.AddOrReplace(transaction.GetHash(), new UnspentOutputs((uint) height, transaction));
        }

        public override Money GetValueIn(Transaction tx)
        {
            return tx.Inputs.RemoveChangePointer().Select(txin => this.GetOutputFor(txin).Value)
                .Sum();
        }
    }
}
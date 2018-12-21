using System.Collections.Generic;
using System.Linq;

namespace NBitcoin.Policy
{
    public class DeStreamMinerTransactionPolicy : ITransactionPolicy
    {
        private DeStreamMinerTransactionPolicy()
        {
        }

        public static DeStreamMinerTransactionPolicy Instance { get; } = new DeStreamMinerTransactionPolicy();

        public virtual TransactionPolicyError[] Check(Transaction transaction, ICoin[] spentCoins)
        {
            spentCoins = spentCoins ?? new ICoin[0];
            var errors = new List<TransactionPolicyError>();

            if (transaction.Version > Transaction.CURRENT_VERSION || transaction.Version < 1)
            {
                errors.Add(new TransactionPolicyError("Invalid transaction version, expected " +
                                                      Transaction.CURRENT_VERSION));
            }

            IEnumerable<IGrouping<OutPoint, IndexedTxIn>> dups = transaction.Inputs.AsIndexedInputs()
                .GroupBy(i => i.PrevOut);
            errors.AddRange(from dup in dups
                select dup.ToArray()
                into duplicates
                where duplicates.Length != 1
                select new DuplicateInputPolicyError(duplicates));

            errors.AddRange(from input in transaction.Inputs.AsIndexedInputs().RemoveChangePointer()
                let coin = spentCoins.FirstOrDefault(s => s.Outpoint == input.PrevOut)
                where coin == null
                select new CoinNotFoundPolicyError(input));

            errors.AddRange(from output in transaction.Outputs.AsCoins()
                where output.Amount < Money.Zero
                select new OutputPolicyError("Output value should not be less than zero", (int) output.Outpoint.N));

            Money fees = transaction.GetFee(spentCoins);
            if (fees != null)
            {
                if (fees < Money.Zero)
                    errors.Add(new NotEnoughFundsPolicyError("Not enough funds in this transaction", -fees));
            }

            TransactionCheckResult check = transaction.Check();
            if (check != TransactionCheckResult.Success)
                errors.Add(new TransactionPolicyError("Context free check of the transaction failed " + check));
            return errors.ToArray();
        }
    }
}
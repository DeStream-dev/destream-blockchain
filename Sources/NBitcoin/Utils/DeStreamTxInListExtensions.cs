using System.Collections.Generic;
using System.Linq;

namespace NBitcoin
{
    public static class DeStreamTxInListExtensions
    {
        public static IEnumerable<TxIn> RemoveChangePointer(this TxInList txInList)
        {
            return txInList.Where(p => p.PrevOut.Hash != uint256.Zero);
        }
    }
}
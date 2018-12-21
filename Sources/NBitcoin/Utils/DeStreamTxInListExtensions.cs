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

        public static IEnumerable<IndexedTxIn> RemoveChangePointer(this IEnumerable<IndexedTxIn> indexedTxIns)
        {
            return indexedTxIns.Where(p => p.PrevOut.Hash != uint256.Zero);
        }

        public static IEnumerable<uint> GetChangePointers(this TxInList txInList)
        {
            return txInList.Where(p => p.PrevOut.Hash == uint256.Zero).Select(p => p.PrevOut.N);
        }

        public static bool IsChangePointer(this TxIn txInList)
        {
            return txInList.PrevOut.Hash == uint256.Zero;
        }
    }
}
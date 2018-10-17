using System.Linq;

namespace NBitcoin
{
    public static class DeStreamTxInListExtensions
    {
        public static TxInList RemoveChangePointer(this TxInList txInList)
        {
            return (TxInList) txInList.Where(p => p.PrevOut.Hash != uint256.Zero);
        }
    }
}
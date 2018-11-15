using System.Collections.Generic;
using System.Linq;

namespace NBitcoin
{
    public class DeStreamCoinSelector : DefaultCoinSelector
    {
        public Script StakeScript { get; set; }

        public DeStreamCoinSelector()
        {
        }

        public DeStreamCoinSelector(int seed) : base(seed)
        {
        }

        public override IEnumerable<ICoin> Select(IEnumerable<ICoin> coins, IMoney target)
        {
            IMoney zero = target.Sub(target);

            var result = new List<ICoin>();
            IMoney total = zero;

            if (target.CompareTo(zero) == 0)
                return result;

            // All CoinStake transactions in wallet got the same ScriptPubKey, and grouping them may lead to
            // spending all coins available for staking. To avoid this, CoinStake transactions are not grouping
            var orderedCoinGroups = coins.GroupBy(c => this.GroupByScriptPubKey
                    ? this.StakeScript != null && c.TxOut.ScriptPubKey == this.StakeScript ? new Key().ScriptPubKey :
                    c.TxOut.ScriptPubKey
                    : new Key().ScriptPubKey)
                .Select(scriptPubKeyCoins => new
                {
                    Amount = scriptPubKeyCoins.Select(c => c.Amount).Sum(zero),
                    Coins = scriptPubKeyCoins.ToList()
                }).OrderBy(c => c.Amount).ToList();


            var targetCoin = orderedCoinGroups
                .FirstOrDefault(c => c.Amount.CompareTo(target) == 0);
            //If any of your UTXO² matches the Target¹ it will be used.
            if (targetCoin != null)
                return targetCoin.Coins;

            foreach (var coinGroup in orderedCoinGroups)
            {
                if (coinGroup.Amount.CompareTo(target) == -1 && total.CompareTo(target) == -1)
                {
                    total = total.Add(coinGroup.Amount);
                    result.AddRange(coinGroup.Coins);
                    //If the "sum of all your UTXO smaller than the Target" happens to match the Target, they will be used. (This is the case if you sweep a complete wallet.)
                    if (total.CompareTo(target) == 0)
                        return result;
                }
                else
                {
                    if (total.CompareTo(target) == -1 && coinGroup.Amount.CompareTo(target) == 1)
                    {
                        //If the "sum of all your UTXO smaller than the Target" doesn't surpass the target, the smallest UTXO greater than your Target will be used.
                        return coinGroup.Coins;
                    }

                    //	Else Bitcoin Core does 1000 rounds of randomly combining unspent transaction outputs until their sum is greater than or equal to the Target. If it happens to find an exact match, it stops early and uses that.
                    //Otherwise it finally settles for the minimum of
                    //the smallest UTXO greater than the Target
                    //the smallest combination of UTXO it discovered in Step 4.
                    var allCoins = orderedCoinGroups.ToArray();
                    IMoney minTotal = null;
                    for (int _ = 0; _ < 1000; _++)
                    {
                        var selection = new List<ICoin>();
                        Utils.Shuffle(allCoins, this._Rand);
                        total = zero;
                        foreach (var coin in allCoins)
                        {
                            selection.AddRange(coin.Coins);
                            total = total.Add(coin.Amount);
                            if (total.CompareTo(target) == 0)
                                return selection;
                            if (total.CompareTo(target) == 1)
                                break;
                        }

                        if (total.CompareTo(target) == -1) return null;
                        if (minTotal == null || total.CompareTo(minTotal) == -1) minTotal = total;
                    }
                }
            }

            return total.CompareTo(target) == -1 ? null : result;
        }
    }
}
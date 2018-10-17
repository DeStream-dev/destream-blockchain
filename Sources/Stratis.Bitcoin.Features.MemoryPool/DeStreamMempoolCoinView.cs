using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NBitcoin;
using NBitcoin.BitcoinCore;
using Stratis.Bitcoin.Features.Consensus;
using Stratis.Bitcoin.Features.Consensus.CoinViews;
using Stratis.Bitcoin.Features.MemoryPool.Interfaces;
using Stratis.Bitcoin.Utilities;

namespace Stratis.Bitcoin.Features.MemoryPool
{
    public class DeStreamMempoolCoinView : MempoolCoinView
    {
        public DeStreamMempoolCoinView(CoinView inner, ITxMempool memPool, SchedulerLock mempoolLock, IMempoolValidator mempoolValidator) : base(inner, memPool, mempoolLock, mempoolValidator)
        {
            this.Set = new DeStreamUnspentOutputSet();
        }

        public override async Task LoadViewAsync(Transaction trx)
        {
            // lookup all ids (duplicate ids are ignored in case a trx spends outputs from the same parent).
            List<uint256> ids = trx.Inputs.Select(n => n.PrevOut.Hash).Distinct().Concat(new[] { trx.GetHash() }).ToList();
            FetchCoinsResponse coins = await this.Inner.FetchCoinsAsync(ids.ToArray());
            // find coins currently in the mempool
            List<Transaction> mempoolcoins = await this.mempoolLock.ReadAsync(() =>
            {
                return this.memPool.MapTx.Values.Where(t => ids.Contains(t.TransactionHash)).Select(s => s.Transaction).ToList();
            });
            IEnumerable<UnspentOutputs> memOutputs = mempoolcoins.Select(s => new UnspentOutputs(TxMempool.MempoolHeight, s));
            coins = new FetchCoinsResponse(coins.UnspentOutputs.Concat(memOutputs).Append(new UnspentOutputs(uint256.Zero, new Coins(new Transaction(), 0))).ToArray(), coins.BlockHash);

            // the UTXO set might have been updated with a recently received block
            // but the block has not yet arrived to the mempool and remove the pending trx
            // from the pool (a race condition), block validation doesn't lock the mempool.
            // its safe to ignore duplicats on the UTXO set as duplicates mean a trx is in
            // a block and the block will soon remove the trx from the pool.
            this.Set.TrySetCoins(coins.UnspentOutputs);
        }
    }
}
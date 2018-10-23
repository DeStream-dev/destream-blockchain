using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Stratis.Bitcoin.Features.Consensus.CoinViews;
using Stratis.Bitcoin.Features.MemoryPool.Interfaces;
using Stratis.Bitcoin.Utilities;

namespace Stratis.Bitcoin.Features.MemoryPool
{
    /// <summary>
    /// Creates <see cref="DeStreamMempoolCoinView"/>
    /// </summary>
    public class DeStreamMempoolManager : MempoolManager
    {
        public DeStreamMempoolManager(MempoolSchedulerLock mempoolLock, ITxMempool memPool, IMempoolValidator validator, IDateTimeProvider dateTimeProvider, MempoolSettings mempoolSettings, IMempoolPersistence mempoolPersistence, CoinView coinView, ILoggerFactory loggerFactory, Network network) : base(mempoolLock, memPool, validator, dateTimeProvider, mempoolSettings, mempoolPersistence, coinView, loggerFactory, network)
        {
        }

        public override async Task<UnspentOutputs> GetUnspentTransactionAsync(uint256 trxid)
        {
            TxMempoolInfo txInfo = await this.InfoAsync(trxid);
            if (txInfo == null)
            {
                return null;
            }
            var memPoolCoinView = new DeStreamMempoolCoinView(this.coinView, this.memPool, this.MempoolLock, this.Validator);
            await memPoolCoinView.LoadViewAsync(txInfo.Trx);
            return memPoolCoinView.GetCoins(trxid);
        }
    }
}
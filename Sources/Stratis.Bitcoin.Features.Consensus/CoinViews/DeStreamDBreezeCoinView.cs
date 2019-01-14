using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Stratis.Bitcoin.Configuration;
using Stratis.Bitcoin.Utilities;

namespace Stratis.Bitcoin.Features.Consensus.CoinViews
{
    /// <inheritdoc cref="DBreezeCoinView" />
    public class DeStreamDBreezeCoinView : DBreezeCoinView, IDisposable
    {
        public DeStreamDBreezeCoinView(Network network, DataFolder dataFolder, IDateTimeProvider dateTimeProvider,
            ILoggerFactory loggerFactory, INodeStats nodeStats) : base(network, dataFolder, dateTimeProvider,
            loggerFactory, nodeStats)
        {
        }

        public DeStreamDBreezeCoinView(Network network, string folder, IDateTimeProvider dateTimeProvider,
            ILoggerFactory loggerFactory, INodeStats nodeStats) : base(network, folder, dateTimeProvider, loggerFactory,
            nodeStats)
        {
        }

        /// <inheritdoc />
        // Instead of ignoring genesis coins, add them to database
        public override Task InitializeAsync()
        {
            var genesis = network.GetGenesis();

            var task = Task.Run(() =>
            {
                using (var transaction = CreateTransaction())
                {
                    transaction.ValuesLazyLoadingIsOn = false;
                    transaction.SynchronizeTables("BlockHash");

                    if (GetTipHash(transaction) != null) return;
                    
                    SetBlockHash(transaction, genesis.GetHash());

                    // Genesis coin is spendable and included to database.
                    transaction.Commit();
                }
            });

            return task;
        }
    }
}
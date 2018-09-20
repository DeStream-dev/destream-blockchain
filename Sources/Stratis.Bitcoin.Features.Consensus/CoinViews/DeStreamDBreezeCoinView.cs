using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Stratis.Bitcoin.Configuration;
using Stratis.Bitcoin.Utilities;
using Transaction = DBreeze.Transactions.Transaction;

namespace Stratis.Bitcoin.Features.Consensus.CoinViews
{
    /// <inheritdoc cref="DBreezeCoinView" />
    public class DeStreamDBreezeCoinView : DBreezeCoinView, IDisposable
    {
        public DeStreamDBreezeCoinView(Network network, DataFolder dataFolder, IDateTimeProvider dateTimeProvider,
            ILoggerFactory loggerFactory) : base(network, dataFolder, dateTimeProvider, loggerFactory)
        {
        }

        public DeStreamDBreezeCoinView(Network network, string folder, IDateTimeProvider dateTimeProvider,
            ILoggerFactory loggerFactory) : base(network, folder, dateTimeProvider, loggerFactory)
        {
        }

        /// <inheritdoc />
        public override Task InitializeAsync()
        {
            this.logger.LogTrace("()");

            Block genesis = this.network.GetGenesis();

            int insertedEntities = 0;

            Task task = Task.Run(() =>
            {
                this.logger.LogTrace("()");

                using (Transaction transaction = this.dbreeze.GetTransaction())
                {
                    transaction.ValuesLazyLoadingIsOn = false;
                    transaction.SynchronizeTables("BlockHash");

                    if (this.GetCurrentHash(transaction) == null)
                    {
                        this.SetBlockHash(transaction, genesis.GetHash());

                        // Genesis coin is spendable and added to the database.
                        foreach (UnspentOutputs unspentOutput in
                            genesis.Transactions.Select(p => new UnspentOutputs(0, p)))
                        {
                            transaction.Insert("Coins", unspentOutput.TransactionId.ToBytes(false),
                                unspentOutput.ToCoins());
                        }

                        insertedEntities += genesis.Transactions.Count;
                        transaction.Commit();
                    }
                }

                this.logger.LogTrace("(-)");
            });

            this.PerformanceCounter.AddInsertedEntities(insertedEntities);
            this.logger.LogTrace("(-)");
            return task;
        }
    }
}
using System.Linq;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Stratis.Bitcoin.Configuration;
using Stratis.Bitcoin.Features.Wallet.Interfaces;
using Stratis.Bitcoin.Utilities;

namespace Stratis.Bitcoin.Features.Wallet
{
    /// <inheritdoc cref="WalletManager" />
    public class DeStreamWalletManager : WalletManager, IDeStreamWalletManager
    {
        public DeStreamWalletManager(ILoggerFactory loggerFactory, Network network, ConcurrentChain chain,
            NodeSettings settings, WalletSettings walletSettings,
            DataFolder dataFolder, IWalletFeePolicy walletFeePolicy, IAsyncLoopFactory asyncLoopFactory,
            INodeLifetime nodeLifetime, IDateTimeProvider dateTimeProvider,
            IBroadcasterManager broadcasterManager = null) :
            base(loggerFactory, network, chain, settings, walletSettings, dataFolder, walletFeePolicy, asyncLoopFactory,
                nodeLifetime, dateTimeProvider, broadcasterManager)
        {
        }

        /// <inheritdoc />
        public override Wallet LoadWallet(string password, string name)
        {
            Wallet result = base.LoadWallet(password, name);

            this.LoadKeysLookupLock();

            return result;
        }

        /// <inheritdoc />
        public void ProcessGenesisBlock()
        {
            foreach (var transactionWithOutput in this.network.GetGenesis().Transactions.SelectMany(p =>
                p.Outputs.Select(q => new {Transaction = p, Output = q}).Where(q =>
                    this.keysLookup.TryGetValue(q.Output.ScriptPubKey, out HdAddress _))))
            {
                this.AddTransactionToWallet(transactionWithOutput.Transaction, transactionWithOutput.Output, 0,
                    this.network.GetGenesis());
            }
        }
    }
}
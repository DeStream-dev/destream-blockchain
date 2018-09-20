using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Stratis.Bitcoin.Configuration;
using Stratis.Bitcoin.Features.Wallet.Interfaces;
using Stratis.Bitcoin.Utilities;

namespace Stratis.Bitcoin.Features.Wallet
{
    /// <inheritdoc />
    public class DeStreamWalletManager : WalletManager
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

        public ConcurrentChain chain { get; set; }

        /// <inheritdoc />
        public override void Start()
        {
            base.Start();

            this.ProcessGenesisBlock();
        }

        /// <inheritdoc />
        public override Wallet LoadWallet(string password, string name)
        {
            Wallet result = base.LoadWallet(password, name);

            this.LoadKeysLookupLock();

            this.ProcessGenesisBlock();

            return result;
        }

        /// <inheritdoc />
        public override Wallet RecoverWallet(string password, string name, string mnemonic, DateTime creationTime,
            string passphrase = null)
        {
            Wallet result = base.RecoverWallet(password, name, mnemonic, creationTime, passphrase);

            this.ProcessGenesisBlock();

            return result;
        }

        /// <summary>
        /// Processes genesis block
        /// </summary>
        private void ProcessGenesisBlock()
        {
            foreach (var transactionWithOutput in this.network.GetGenesis().Transactions.SelectMany(p =>
                p.Outputs.Select(q => new { Transaction = p, Output = q }).Where(q =>
                    this.keysLookup.TryGetValue(q.Output.ScriptPubKey, out HdAddress _))))
            {
                this.AddTransactionToWallet(transactionWithOutput.Transaction, transactionWithOutput.Output, 0,
                    this.network.GetGenesis());
            }
        }
    }
}
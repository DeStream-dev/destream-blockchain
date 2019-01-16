using Microsoft.Extensions.Logging;
using NBitcoin;
using Stratis.Bitcoin.Features.BlockStore;
using Stratis.Bitcoin.Features.Wallet.Interfaces;
using Stratis.Bitcoin.Interfaces;
using Stratis.Bitcoin.Utilities;

namespace Stratis.Bitcoin.Features.Wallet
{
    public class DeStreamWalletSyncManager : WalletSyncManager
    {
        private readonly IDeStreamWalletManager _deStreamWalletManager;

        public DeStreamWalletSyncManager(ILoggerFactory loggerFactory, IDeStreamWalletManager walletManager,
            ConcurrentChain chain, Network network, IBlockStore blockStore, StoreSettings storeSettings,
            INodeLifetime nodeLifetime) : base(loggerFactory, walletManager, chain, network, blockStore, storeSettings,
            nodeLifetime)
        {
            this._deStreamWalletManager = walletManager;
        }

        public override void SyncFromHeight(int height)
        {
            base.SyncFromHeight(height);

            // Wallet's initial state - height 0 and no blocks processed,
            // but there may be transactions at height 0.
            // This function is called with next unprocessed block height,
            // so, processing of genesis block is required on height = 1.
            if (height > 1) return;

            this._deStreamWalletManager.ProcessGenesisBlock();
            this.logger.LogTrace("Genesis block processed");
        }
    }
}
using Microsoft.Extensions.Logging;
using NBitcoin;
using Stratis.Bitcoin.Features.Notifications.Interfaces;
using Stratis.Bitcoin.Features.Wallet.Interfaces;
using Stratis.Bitcoin.Signals;
using Stratis.Bitcoin.Utilities;

namespace Stratis.Bitcoin.Features.LightWallet
{
    public class DeStreamLightWalletSyncManager : LightWalletSyncManager
    {
        private readonly IDeStreamWalletManager _deStreamWalletManager;

        public DeStreamLightWalletSyncManager(ILoggerFactory loggerFactory, ConcurrentChain chain, Network network,
            IBlockNotification blockNotification, ISignals signals, INodeLifetime nodeLifetime,
            IAsyncLoopFactory asyncLoopFactory, IDeStreamWalletManager deStreamWalletManager) : base(loggerFactory,
            deStreamWalletManager, chain, network, blockNotification, signals, nodeLifetime, asyncLoopFactory)
        {
            this._deStreamWalletManager = deStreamWalletManager;
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
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Stratis.Bitcoin.Configuration;
using Stratis.Bitcoin.Features.Wallet.Interfaces;
using Stratis.Bitcoin.Utilities;

namespace Stratis.Bitcoin.Features.Wallet
{
    public class DeStreamWalletManager : WalletManager
    {
        public DeStreamWalletManager(ILoggerFactory loggerFactory, Network network, ConcurrentChain chain, NodeSettings settings, WalletSettings walletSettings,
            DataFolder dataFolder, IWalletFeePolicy walletFeePolicy, IAsyncLoopFactory asyncLoopFactory, INodeLifetime nodeLifetime, IDateTimeProvider dateTimeProvider,
            IBroadcasterManager broadcasterManager = null) :
            base(loggerFactory, network, chain, settings, walletSettings, dataFolder, walletFeePolicy, asyncLoopFactory, nodeLifetime, dateTimeProvider, broadcasterManager)
        { }

        public ConcurrentChain chain { get; set; }
    }
}

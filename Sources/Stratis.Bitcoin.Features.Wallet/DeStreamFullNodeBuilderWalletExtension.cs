using Microsoft.Extensions.DependencyInjection;
using Stratis.Bitcoin.Builder;
using Stratis.Bitcoin.Configuration.Logging;
using Stratis.Bitcoin.Features.BlockStore;
using Stratis.Bitcoin.Features.MemoryPool;
using Stratis.Bitcoin.Features.RPC;
using Stratis.Bitcoin.Features.Wallet.Broadcasting;
using Stratis.Bitcoin.Features.Wallet.Controllers;
using Stratis.Bitcoin.Features.Wallet.Interfaces;

namespace Stratis.Bitcoin.Features.Wallet
{
    /// <summary>
    ///     A class providing extension methods for <see cref="IFullNodeBuilder" />.
    /// </summary>
    public static class DeStreamFullNodeBuilderWalletExtension
    {
        public static IFullNodeBuilder UseDeStreamWallet(this IFullNodeBuilder fullNodeBuilder)
        {
            LoggingConfiguration.RegisterFeatureNamespace<WalletFeature>("wallet");

            fullNodeBuilder.ConfigureFeature(features =>
            {
                features
                    .AddFeature<WalletFeature>()
                    .DependOn<MempoolFeature>()
                    .DependOn<BlockStoreFeature>()
                    .DependOn<RPCFeature>()
                    .FeatureServices(services =>
                    {
                        services.AddSingleton<IWalletSyncManager, DeStreamWalletSyncManager>();
                        services.AddSingleton<IWalletTransactionHandler, DeStreamWalletTransactionHandler>();
                        services.AddSingleton<IDeStreamWalletManager, DeStreamWalletManager>();
                        services.AddSingleton<IWalletManager>(p => p.GetService<IDeStreamWalletManager>());
                        services.AddSingleton<IWalletFeePolicy, WalletFeePolicy>();
                        services.AddSingleton<WalletController>();
                        services.AddSingleton<DeStreamWalletController>();
                        services.AddSingleton<WalletRPCController>();
                        services.AddSingleton<IBroadcasterManager, FullNodeBroadcasterManager>();
                        services.AddSingleton<BroadcasterBehavior>();
                        services.AddSingleton<WalletSettings>();
                    });
            });

            return fullNodeBuilder;
        }
    }
}
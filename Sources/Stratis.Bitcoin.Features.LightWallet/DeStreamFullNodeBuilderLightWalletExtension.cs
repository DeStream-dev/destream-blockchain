using Microsoft.Extensions.DependencyInjection;
using Stratis.Bitcoin.Builder;
using Stratis.Bitcoin.Features.Consensus;
using Stratis.Bitcoin.Features.LightWallet.Broadcasting;
using Stratis.Bitcoin.Features.Notifications;
using Stratis.Bitcoin.Features.Wallet;
using Stratis.Bitcoin.Features.Wallet.Broadcasting;
using Stratis.Bitcoin.Features.Wallet.Controllers;
using Stratis.Bitcoin.Features.Wallet.Interfaces;
using Stratis.Bitcoin.Interfaces;
using Stratis.Bitcoin.Utilities;

namespace Stratis.Bitcoin.Features.LightWallet
{
    public static class DeStreamFullNodeBuilderLightWalletExtension
    {
        public static IFullNodeBuilder UseDeStreamLightWallet(this IFullNodeBuilder fullNodeBuilder)
        {
            fullNodeBuilder.ConfigureFeature(features =>
            {
                // Required to GetStakeMinConfirmations
                fullNodeBuilder.Network.Consensus.Options = new DeStreamPosConsensusOptions();

                features
                    .AddFeature<LightWalletFeature>()
                    .DependOn<BlockNotificationFeature>()
                    .DependOn<TransactionNotificationFeature>()
                    .FeatureServices(services =>
                    {
                        services.AddSingleton<IWalletSyncManager, DeStreamLightWalletSyncManager>();
                        services.AddSingleton<IWalletTransactionHandler, DeStreamWalletTransactionHandler>();
                        services.AddSingleton<IDeStreamWalletManager, DeStreamWalletManager>();
                        services.AddSingleton<IWalletManager>(p => p.GetService<IDeStreamWalletManager>());
                        if (fullNodeBuilder.Network.IsBitcoin())
                            services.AddSingleton<IWalletFeePolicy, LightWalletBitcoinExternalFeePolicy>();
                        else
                            services.AddSingleton<IWalletFeePolicy, LightWalletFixedFeePolicy>();
                        services.AddSingleton<WalletController>();
                        services.AddSingleton<DeStreamWalletController>();
                        services.AddSingleton<IBroadcasterManager, LightWalletBroadcasterManager>();
                        services.AddSingleton<BroadcasterBehavior>();
                        services.AddSingleton<IInitialBlockDownloadState, LightWalletInitialBlockDownloadState>();
                        services.AddSingleton<WalletSettings>();
                    });
            });

            return fullNodeBuilder;
        }
    }
}
using Microsoft.Extensions.DependencyInjection;
using Stratis.Bitcoin.Builder;
using Stratis.Bitcoin.Configuration.Logging;
using Stratis.Bitcoin.Features.MemoryPool;
using Stratis.Bitcoin.Features.Miner.Controllers;
using Stratis.Bitcoin.Features.Miner.Interfaces;
using Stratis.Bitcoin.Features.RPC;
using Stratis.Bitcoin.Features.Wallet;
using Stratis.Bitcoin.Mining;

namespace Stratis.Bitcoin.Features.Miner
{
    public static class DeStreamFullNodeBuilderMinerExtension
    {
        public static IFullNodeBuilder AddDeStreamPowMining(this IFullNodeBuilder fullNodeBuilder)
        {
            LoggingConfiguration.RegisterFeatureNamespace<MiningFeature>("mining");

            fullNodeBuilder.ConfigureFeature(features =>
            {
                features
                    .AddFeature<MiningFeature>()
                    .DependOn<MempoolFeature>()
                    .DependOn<RPCFeature>()
                    .DependOn<WalletFeature>()
                    .FeatureServices(services =>
                    {
                        services.AddSingleton<IPowMining, PowMining>();
                        services.AddSingleton<IBlockProvider, BlockProvider>();
                        services.AddSingleton<BlockDefinition, DeStreamPowBlockDefinition>();
                        services.AddSingleton<MinerController>();
                        services.AddSingleton<MiningRPCController>();
                        services.AddSingleton<MinerSettings>();
                    });
            });

            return fullNodeBuilder;
        }
        
        public static IFullNodeBuilder AddDeStreamPowPosMining(this IFullNodeBuilder fullNodeBuilder)
        {

            LoggingConfiguration.RegisterFeatureNamespace<MiningFeature>("mining");

            fullNodeBuilder.ConfigureFeature(features =>
            {
                features
                    .AddFeature<MiningFeature>()
                    .DependOn<MempoolFeature>()
                    .DependOn<RPCFeature>()
                    .DependOn<WalletFeature>()
                    .FeatureServices(services =>
                    {
                        services.AddSingleton<IPowMining, PowMining>();
                        services.AddSingleton<IPosMinting, DeStreamPosMinting>();
                        services.AddSingleton<IBlockProvider, BlockProvider>();
                        services.AddSingleton<BlockDefinition, DeStreamPowBlockDefinition>();
                        services.AddSingleton<BlockDefinition, DeStreamPosPowBlockDefinition>();
                        services.AddSingleton<BlockDefinition, PosBlockDefinition>();
                        services.AddSingleton<MinerController>();
                        services.AddSingleton<MiningRPCController>();
                        services.AddSingleton<MinerSettings>();
                    });
            });

            return fullNodeBuilder;
        }
    }
}
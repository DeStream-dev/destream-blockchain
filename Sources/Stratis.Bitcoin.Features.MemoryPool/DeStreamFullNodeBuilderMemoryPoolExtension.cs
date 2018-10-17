using Microsoft.Extensions.DependencyInjection;
using Stratis.Bitcoin.Builder;
using Stratis.Bitcoin.Configuration.Logging;
using Stratis.Bitcoin.Features.Consensus;
using Stratis.Bitcoin.Features.MemoryPool.Fee;
using Stratis.Bitcoin.Features.MemoryPool.Interfaces;
using Stratis.Bitcoin.Interfaces;

namespace Stratis.Bitcoin.Features.MemoryPool
{
    /// <summary>
    ///     A class providing extension methods for <see cref="IFullNodeBuilder" />.
    /// </summary>
    public static class DeStreamFullNodeBuilderMemoryPoolExtension
    {
        /// <summary>
        /// Include the memory pool feature and related services in the full node.
        /// </summary>
        /// <param name="fullNodeBuilder">Full node builder.</param>
        /// <returns>Full node builder.</returns>
        public static IFullNodeBuilder UseDeStreamMempool(this IFullNodeBuilder fullNodeBuilder)
        {
            LoggingConfiguration.RegisterFeatureNamespace<MempoolFeature>("mempool");
            LoggingConfiguration.RegisterFeatureNamespace<BlockPolicyEstimator>("estimatefee");

            fullNodeBuilder.ConfigureFeature(features =>
            {
                features
                    .AddFeature<MempoolFeature>()
                    .DependOn<ConsensusFeature>()
                    .FeatureServices(services =>
                    {
                        services.AddSingleton<MempoolSchedulerLock>();
                        services.AddSingleton<ITxMempool, TxMempool>();
                        services.AddSingleton<BlockPolicyEstimator>();
                        services.AddSingleton<IMempoolValidator, DeStreamMempoolValidator>();
                        services.AddSingleton<MempoolOrphans>();
                        services.AddSingleton<DeStreamMempoolManager>();
                        services.AddSingleton<IPooledTransaction, DeStreamMempoolManager>();
                        services.AddSingleton<IPooledGetUnspentTransaction, DeStreamMempoolManager>();
                        services.AddSingleton<MempoolBehavior>();
                        services.AddSingleton<MempoolSignaled>();
                        services.AddSingleton<IMempoolPersistence, MempoolPersistence>();
                        services.AddSingleton<MempoolController>();
                        services.AddSingleton<MempoolSettings>();
                    });
            });

            return fullNodeBuilder;
        }
    }
}
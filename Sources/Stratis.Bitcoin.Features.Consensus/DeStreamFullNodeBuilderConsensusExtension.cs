using Microsoft.Extensions.DependencyInjection;
using Stratis.Bitcoin.BlockPulling;
using Stratis.Bitcoin.Builder;
using Stratis.Bitcoin.Configuration.Logging;
using Stratis.Bitcoin.Configuration.Settings;
using Stratis.Bitcoin.Consensus;
using Stratis.Bitcoin.Consensus.Rules;
using Stratis.Bitcoin.Features.Consensus.CoinViews;
using Stratis.Bitcoin.Features.Consensus.Interfaces;
using Stratis.Bitcoin.Features.Consensus.Rules;
using Stratis.Bitcoin.Interfaces;

namespace Stratis.Bitcoin.Features.Consensus
{
    /// <summary>
    ///     A class providing extension methods for <see cref="IFullNodeBuilder" />.
    /// </summary>
    public static class DeStreamFullNodeBuilderConsensusExtension
    {
        public static IFullNodeBuilder UseDeStreamPowConsensus(this IFullNodeBuilder fullNodeBuilder)
        {
            LoggingConfiguration.RegisterFeatureNamespace<ConsensusFeature>("consensus");
            LoggingConfiguration.RegisterFeatureClass<ConsensusStats>("bench");

            fullNodeBuilder.ConfigureFeature(features =>
            {
                features
                    .AddFeature<ConsensusFeature>()
                    .FeatureServices(services =>
                    {
                        // TODO: this should be set on the network build
                        fullNodeBuilder.Network.Consensus.Options = new PowConsensusOptions();

                        services.AddSingleton<ICheckpoints, Checkpoints>();
                        services.AddSingleton<NBitcoin.Consensus.ConsensusOptions, PowConsensusOptions>();
                        services.AddSingleton<DBreezeCoinView, DeStreamDBreezeCoinView>();
                        services.AddSingleton<CoinView, CachedCoinView>();
                        services.AddSingleton<LookaheadBlockPuller>()
                            .AddSingleton<ILookaheadBlockPuller, LookaheadBlockPuller>(provider =>
                                provider.GetService<LookaheadBlockPuller>());
                        ;
                        services.AddSingleton<IConsensusLoop, ConsensusLoop>()
                            .AddSingleton<INetworkDifficulty, ConsensusLoop>(provider =>
                                provider.GetService<IConsensusLoop>() as ConsensusLoop)
                            .AddSingleton<IGetUnspentTransaction, ConsensusLoop>(provider =>
                                provider.GetService<IConsensusLoop>() as ConsensusLoop);
                        services.AddSingleton<IInitialBlockDownloadState, InitialBlockDownloadState>();
                        services.AddSingleton<ConsensusController>();
                        services.AddSingleton<ConsensusStats>();
                        services.AddSingleton<ConsensusSettings>();
                        services.AddSingleton<IConsensusRules, PowConsensusRules>();
                        services
                            .AddSingleton<IRuleRegistration,
                                FullNodeBuilderConsensusExtension.PowConsensusRulesRegistration>();
                    });
            });

            return fullNodeBuilder;
        }

        public static IFullNodeBuilder UseDeStreamPosConsensus(this IFullNodeBuilder fullNodeBuilder)
        {
            LoggingConfiguration.RegisterFeatureNamespace<ConsensusFeature>("consensus");
            LoggingConfiguration.RegisterFeatureClass<ConsensusStats>("bench");

            fullNodeBuilder.ConfigureFeature(features =>
            {
                features
                    .AddFeature<ConsensusFeature>()
                    .FeatureServices(services =>
                    {
                        fullNodeBuilder.Network.Consensus.Options = new PosConsensusOptions();

                        services.AddSingleton<ICheckpoints, Checkpoints>();
                        services.AddSingleton<DBreezeCoinView, DeStreamDBreezeCoinView>();
                        services.AddSingleton<CoinView, CachedCoinView>();
                        services.AddSingleton<LookaheadBlockPuller>()
                            .AddSingleton<ILookaheadBlockPuller, LookaheadBlockPuller>(provider =>
                                provider.GetService<LookaheadBlockPuller>());
                        ;
                        services.AddSingleton<IConsensusLoop, ConsensusLoop>()
                            .AddSingleton<INetworkDifficulty, ConsensusLoop>(provider =>
                                provider.GetService<IConsensusLoop>() as ConsensusLoop)
                            .AddSingleton<IGetUnspentTransaction, ConsensusLoop>(provider =>
                                provider.GetService<IConsensusLoop>() as ConsensusLoop);
                        services.AddSingleton<StakeChainStore>()
                            .AddSingleton<IStakeChain, StakeChainStore>(provider =>
                                provider.GetService<StakeChainStore>());
                        services.AddSingleton<IStakeValidator, StakeValidator>();
                        services.AddSingleton<IInitialBlockDownloadState, InitialBlockDownloadState>();
                        services.AddSingleton<ConsensusController>();
                        services.AddSingleton<ConsensusStats>();
                        services.AddSingleton<ConsensusSettings>();
                        services.AddSingleton<IConsensusRules, PosConsensusRules>();
                        services
                            .AddSingleton<IRuleRegistration,
                                FullNodeBuilderConsensusExtension.PosConsensusRulesRegistration>();
                    });
            });

            return fullNodeBuilder;
        }
    }
}
using System.Collections.Generic;
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
using Stratis.Bitcoin.Features.Consensus.Rules.CommonRules;
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
                        services.AddSingleton<IConsensusRules, DeStreamPowConsensusRules>();
                        services
                            .AddSingleton<IRuleRegistration,
                                DeStreamPowConsensusRulesRegistration>();
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
                        fullNodeBuilder.Network.Consensus.Options = new DeStreamPosConsensusOptions();

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
                        services.AddSingleton<IConsensusRules, DeStreamPosConsensusRules>();
                        services
                            .AddSingleton<IRuleRegistration,
                                DeStreamPosConsensusRulesRegistration>();
                    });
            });

            return fullNodeBuilder;
        }

        private class DeStreamPowConsensusRulesRegistration : IRuleRegistration
        {
            public IEnumerable<ConsensusRule> GetRules()
            {
                return new List<ConsensusRule>
                {
                    new BlockHeaderRule(),

                    // rules that are inside the method CheckBlockHeader
                    new CalculateWorkRule(),

                    // rules that are inside the method ContextualCheckBlockHeader
                    new CheckpointsRule(),
                    new AssumeValidRule(),
                    new BlockHeaderPowContextualRule(),

                    // rules that are inside the method ContextualCheckBlock
                    new TransactionLocktimeActivationRule(), // implements BIP113
                    new CoinbaseHeightActivationRule(), // implements BIP34
                    new WitnessCommitmentsRule(), // BIP141, BIP144
                    new BlockSizeRule(),

                    // rules that are inside the method CheckBlock
                    new BlockMerkleRootRule(),
                    new EnsureCoinbaseRule(),
                    new CheckPowTransactionRule(),
                    new CheckSigOpsRule(),

                    // rules that require the store to be loaded (coinview)
                    new DeStreamLoadCoinviewRule(),
                    new DeStreamFundsPreservationRule(),
                    new TransactionDuplicationActivationRule(), // implements BIP30
                    new DeStreamPowCoinviewRule(), // implements BIP68, MaxSigOps and BlockReward calculation
                    new DeStreamBlockFeeRule()
                    
                };
            }
        }

        private class DeStreamPosConsensusRulesRegistration : IRuleRegistration
        {
            public IEnumerable<ConsensusRule> GetRules()
            {
                return new List<ConsensusRule>
                {
                    new BlockHeaderRule(),

                    // rules that are inside the method CheckBlockHeader
                    new CalculateStakeRule(),

                    // rules that are inside the method ContextualCheckBlockHeader
                    new CheckpointsRule(),
                    new AssumeValidRule(),
                    new BlockHeaderPowContextualRule(),
                    new BlockHeaderPosContextualRule(),

                    // rules that are inside the method ContextualCheckBlock
                    new TransactionLocktimeActivationRule(), // implements BIP113
                    new CoinbaseHeightActivationRule(), // implements BIP34
                    new WitnessCommitmentsRule(), // BIP141, BIP144 
                    new BlockSizeRule(),

                    new PosBlockContextRule(), // TODO: this rule needs to be implemented

                    // rules that are inside the method CheckBlock
                    new BlockMerkleRootRule(),
                    new EnsureCoinbaseRule(),
                    new CheckPowTransactionRule(),
                    new CheckPosTransactionRule(),
                    new CheckSigOpsRule(),
                    new PosFutureDriftRule(),
                    new PosCoinstakeRule(),
                    new PosBlockSignatureRule(),

                    // rules that require the store to be loaded (coinview)
                    new DeStreamLoadCoinviewRule(),
                    new DeStreamFundsPreservationRule(),
                    new TransactionDuplicationActivationRule(), // implements BIP30
                    new DeStreamPosCoinviewRule(), // implements BIP68, MaxSigOps and BlockReward calculation
                    new DeStreamBlockFeeRule()
                };
            }
        }
    }
}
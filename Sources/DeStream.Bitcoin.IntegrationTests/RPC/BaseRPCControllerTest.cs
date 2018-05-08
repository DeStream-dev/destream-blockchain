using DeStream.Bitcoin.Builder;
using DeStream.Bitcoin.Configuration;
using DeStream.Bitcoin.Features.BlockStore;
using DeStream.Bitcoin.Features.Consensus;
using DeStream.Bitcoin.Features.MemoryPool;
using DeStream.Bitcoin.Features.RPC;
using DeStream.Bitcoin.Tests;

namespace DeStream.Bitcoin.IntegrationTests.RPC
{
    /// <summary>
    /// Base class for RPC tests.
    /// </summary>
    public abstract class BaseRPCControllerTest : TestBase
    {
        /// <summary>
        /// Builds a node with basic services and RPC enabled.
        /// </summary>
        /// <param name="dir">Data directory that the node should use.</param>
        /// <returns>Interface to the newly built node.</returns>
        public IFullNode BuildServicedNode(string dir)
        {
            NodeSettings nodeSettings = new NodeSettings(args:new string[] { $"-datadir={dir}" });
            var fullNodeBuilder = new FullNodeBuilder(nodeSettings);
            IFullNode fullNode = fullNodeBuilder
                .UsePowConsensus()
                .UseBlockStore()
                .UseMempool()
                .AddRPC()
                .Build();

            return fullNode;
        }
    }
}

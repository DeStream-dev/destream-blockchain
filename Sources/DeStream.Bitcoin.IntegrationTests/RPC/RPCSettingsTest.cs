using NBitcoin;
using DeStream.Bitcoin.Builder;
using DeStream.Bitcoin.Configuration;
using DeStream.Bitcoin.Features.Consensus;
using DeStream.Bitcoin.Features.RPC;
using DeStream.Bitcoin.Tests;
using Xunit;

namespace DeStream.Bitcoin.IntegrationTests.RPC
{
    public class RPCSettingsTest : TestBase
    {
        [Fact]
        public void CanSpecifyRPCSettings()
        {
            var initialBlockSignature = Block.BlockSignature;

            try
            {
                Block.BlockSignature = false;
                var dir = CreateTestDir(this);

                NodeSettings nodeSettings = new NodeSettings(args:new string[] { $"-datadir={dir}" });

                var node = new FullNodeBuilder()
                    .UseNodeSettings(nodeSettings)
                    .UsePowConsensus()
                    .AddRPC(x =>
                    {
                        x.RpcUser = "abc";
                        x.RpcPassword = "def";
                        x.RPCPort = 91;
                    })
                    .Build();

                var settings = node.NodeService<RpcSettings>();

                settings.Load(nodeSettings);

                Assert.Equal("abc", settings.RpcUser);
                Assert.Equal("def", settings.RpcPassword);
                Assert.Equal(91, settings.RPCPort);
            }
            finally
            {
                Block.BlockSignature = initialBlockSignature;
            }

        }
    }
}

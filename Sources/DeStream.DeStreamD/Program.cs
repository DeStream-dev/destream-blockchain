using System;
using System.Linq;
using System.Threading.Tasks;
using DeStream.Stratis.Bitcoin.Configuration;
using NBitcoin;
using NBitcoin.Protocol;
using Stratis.Bitcoin;
using Stratis.Bitcoin.Builder;
using Stratis.Bitcoin.Features.Api;
using Stratis.Bitcoin.Features.BlockStore;
using Stratis.Bitcoin.Features.Consensus;
using Stratis.Bitcoin.Features.MemoryPool;
using Stratis.Bitcoin.Features.Miner;
using Stratis.Bitcoin.Features.RPC;
using Stratis.Bitcoin.Features.Wallet;
using Stratis.Bitcoin.Utilities;

namespace DeStream.DeStreamD
{
    public class Program
    {
        public static void Main(string[] args)
        {
            MainAsync(args).Wait();
        }

        public static async Task MainAsync(string[] args)
        {
            try
            {
                Network network = args.Contains("-testnet") ? Network.DeStreamTest : Network.DeStreamMain;
                
                var nodeSettings = new DeStreamNodeSettings(network, ProtocolVersion.ALT_PROTOCOL_VERSION, args: args,
                    loadConfiguration: false);

                Console.WriteLine($"current network: {network.Name}");

                // NOTES: running BTC and STRAT side by side is not possible yet as the flags for serialization are static
                IFullNode node = new FullNodeBuilder()
                    .UseNodeSettings(nodeSettings)
                    .UseBlockStore()
                    .UseDeStreamPosConsensus()
                    .UseDeStreamMempool()
                    .UseDeStreamWallet()
                    .AddDeStreamPowPosMining()
                    .UseApi()
                    .AddRPC()
                    .Build();

                if (node != null)
                    await node.RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("There was a problem initializing the node. Details: '{0}'", ex.Message);
            }
        }
    }
}
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using NBitcoin;
using NBitcoin.Protocol;
using DeStream.Bitcoin.Builder;
using DeStream.Bitcoin.Configuration;
using DeStream.Bitcoin.Features.Api;
using DeStream.Bitcoin.Features.BlockStore;
using DeStream.Bitcoin.Features.Consensus;
using DeStream.Bitcoin.Features.MemoryPool;
using DeStream.Bitcoin.Features.Miner;
using DeStream.Bitcoin.Features.RPC;
using DeStream.Bitcoin.Features.Wallet;
using DeStream.Bitcoin.Utilities;

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
                Network network = null;
                if (args.Contains("-testnet"))
                    network = Network.DeStreamTest;
                else if (args.Contains("-destreamtest"))
                    network = Network.DestreamTest;
                else if (args.Contains("-destreamtestserver"))
                    network = Network.DestreamTestServer;
                else
                    network = Network.DeStreamMain;
                NodeSettings nodeSettings = new NodeSettings(network, ProtocolVersion.ALT_PROTOCOL_VERSION, args:args, loadConfiguration:false);

                Console.WriteLine($"current network: {network.Name}");

                // NOTES: running BTC and STRAT side by side is not possible yet as the flags for serialization are static
                var node = new FullNodeBuilder()
                    .UseNodeSettings(nodeSettings)
                    .UsePosConsensus()
                    .UseBlockStore()
                    .UseMempool()
                    .UseWallet()
                    .AddPowPosMining()
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

using System;
using System.Linq;
using System.Threading.Tasks;
using NBitcoin;
using NBitcoin.Protocol;
using DeStream.Bitcoin;
using DeStream.Bitcoin.Builder;
using DeStream.Bitcoin.Configuration;
using DeStream.Bitcoin.Features.Api;
using DeStream.Bitcoin.Features.BlockStore;
using DeStream.Bitcoin.Features.Consensus;
using DeStream.Bitcoin.Features.Dns;
using DeStream.Bitcoin.Features.MemoryPool;
using DeStream.Bitcoin.Features.Miner;
using DeStream.Bitcoin.Features.RPC;
using DeStream.Bitcoin.Features.Wallet;
using DeStream.Bitcoin.Utilities;

namespace DeStream.DeStreamDnsD
{
    /// <summary>
    /// Main entry point.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// The entry point for the DeStream Dns process.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        public static void Main(string[] args)
        {
            MainAsync(args).Wait();
        }

        /// <summary>
        /// The async entry point for the DeStream Dns process.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        /// <returns>A task used to await the operation.</returns>
        public static async Task MainAsync(string[] args)
        {
            try
            {
                Network network = args.Contains("-testnet") ? Network.DeStreamTest : Network.DeStreamMain;
                NodeSettings nodeSettings = new NodeSettings(network, ProtocolVersion.ALT_PROTOCOL_VERSION, args:args, loadConfiguration:false);

                Action<DnsSettings> serviceTest = (s) =>
                {
                    if (string.IsNullOrWhiteSpace(s.DnsHostName) || string.IsNullOrWhiteSpace(s.DnsNameServer) || string.IsNullOrWhiteSpace(s.DnsMailBox))
                        throw new ConfigurationException("When running as a DNS Seed service, the -dnshostname, -dnsnameserver and -dnsmailbox arguments must be specified on the command line.");
                };

                // Run as a full node with DNS or just a DNS service?
                IFullNode node;
                if (args.Contains("-dnsfullnode"))
                {
                    // Build the Dns full node.
                    node = new FullNodeBuilder()
                        .UseNodeSettings(nodeSettings)
                        .UsePosConsensus()
                        .UseBlockStore()
                        .UseMempool()
                        .UseWallet()
                        .AddPowPosMining()
                        .UseApi()
                        .AddRPC()
                        .UseDns(serviceTest)
                        .Build();
                }
                else
                {
                    // Build the Dns node.
                    node = new FullNodeBuilder()
                        .UseNodeSettings(nodeSettings)
                        .UsePosConsensus()
                        .UseApi()
                        .AddRPC()
                        .UseDns(serviceTest)
                        .Build();
                }

                // Run node.
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

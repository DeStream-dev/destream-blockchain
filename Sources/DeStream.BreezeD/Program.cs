using System;
using System.Linq;
using System.Threading.Tasks;
using NBitcoin;
using NBitcoin.Protocol;
using DeStream.Bitcoin;
using DeStream.Bitcoin.Builder;
using DeStream.Bitcoin.Configuration;
using DeStream.Bitcoin.Features.Api;
using DeStream.Bitcoin.Features.LightWallet;
using DeStream.Bitcoin.Features.Notifications;
using DeStream.Bitcoin.Utilities;

namespace DeStream.BreezeD
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
                // Get the API uri.
                var isTestNet = args.Contains("-testnet");
                var isDeStream = args.Contains("destream");
                var isDestreamTest = args.Contains("-destreamtest");

                var agent = "Breeze";

                NodeSettings nodeSettings;

                if(isDestreamTest)
                {
                    Network network = Network.DestreamTest;
                    nodeSettings = new NodeSettings(network, ProtocolVersion.ALT_PROTOCOL_VERSION, agent, args: args, loadConfiguration: false);
                }
                else if (isDeStream)
                {
                    Network network = isTestNet ? Network.DeStreamTest : Network.DeStreamMain;
                    if (isTestNet)
                        args = args.Append("-addnode=51.141.28.47").ToArray(); // TODO: fix this temp hack

                    nodeSettings = new NodeSettings(network, ProtocolVersion.ALT_PROTOCOL_VERSION, agent, args:args, loadConfiguration:false);
                }
                else
                {
                    nodeSettings = new NodeSettings(agent: agent, args: args, loadConfiguration:false);
                }

                IFullNodeBuilder fullNodeBuilder = new FullNodeBuilder()
                    .UseNodeSettings(nodeSettings)
                    .UseLightWallet()
                    .UseBlockNotification()
                    .UseTransactionNotification()
                    .UseApi();

                IFullNode node = fullNodeBuilder.Build();

                // Start Full Node - this will also start the API.
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

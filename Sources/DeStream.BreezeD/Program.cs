using System;
using System.Linq;
using System.Threading.Tasks;
using NBitcoin.Protocol;
using Stratis.Bitcoin;
using Stratis.Bitcoin.Builder;
using Stratis.Bitcoin.Configuration;
using Stratis.Bitcoin.Features.Api;
using Stratis.Bitcoin.Features.LightWallet;
using Stratis.Bitcoin.Features.Notifications;
using Stratis.Bitcoin.Networks;
using Stratis.Bitcoin.Utilities;

namespace DeStream.BreezeD
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            try
            {
                string agent = "Breeze";

                NodeSettings nodeSettings;

                nodeSettings = new NodeSettings(networksSelector: Networks.DeStream,
                    protocolVersion: ProtocolVersion.ALT_PROTOCOL_VERSION, agent: agent, args: args);
                
                IFullNodeBuilder fullNodeBuilder = new FullNodeBuilder()
                    .UseNodeSettings(nodeSettings)
                    .UseDeStreamLightWallet()
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
                Console.WriteLine("There was a problem initializing the node. Details: '{0}'", ex.ToString());
            }
        }
    }
}
using System;
using System.Threading.Tasks;
using DeStream.Bitcoin.Builder;
using DeStream.Bitcoin.Configuration;
using DeStream.Bitcoin.Features.BlockStore;
using DeStream.Bitcoin.Features.Consensus;
using DeStream.Bitcoin.Features.MemoryPool;
using DeStream.Bitcoin.Features.Miner;
using DeStream.Bitcoin.Features.RPC;
using DeStream.Bitcoin.Features.Wallet;
using DeStream.Bitcoin.Utilities;

namespace DeStream.BitcoinD
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
                NodeSettings nodeSettings = new NodeSettings(args:args, loadConfiguration:false);

                var node = new FullNodeBuilder()
                    .UseNodeSettings(nodeSettings)
                    .UsePowConsensus()
                    .UseBlockStore()
                    .UseMempool()
                    .AddMining()
                    .AddRPC()
                    .UseWallet()
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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DeStream.Stratis.Bitcoin.Configuration;
using Moq;
using NBitcoin;
using NBitcoin.Networks;
using NBitcoin.Protocol;
using Stratis.Bitcoin;
using Stratis.Bitcoin.Builder;
using Stratis.Bitcoin.Configuration;
using Stratis.Bitcoin.Features.Api;
using Stratis.Bitcoin.Features.BlockStore;
using Stratis.Bitcoin.Features.Consensus;
using Stratis.Bitcoin.Features.MemoryPool;
using Stratis.Bitcoin.Features.Miner;
using Stratis.Bitcoin.Features.Miner.Interfaces;
using Stratis.Bitcoin.Features.RPC;
using Stratis.Bitcoin.Features.Wallet;
using Stratis.Bitcoin.Features.Wallet.Controllers;
using Stratis.Bitcoin.Features.Wallet.Interfaces;
using Stratis.Bitcoin.Features.Wallet.Models;
using Stratis.Bitcoin.IntegrationTests.Common;
using Stratis.Bitcoin.IntegrationTests.Common.EnvironmentMockUpHelpers;
using Stratis.Bitcoin.Interfaces;
using Stratis.Bitcoin.Tests.Wallet.Common;
using Stratis.Bitcoin.Utilities;
using static Stratis.Bitcoin.BlockPulling.BlockPuller;

namespace DeStream.DeStreamD.ForTest
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
                else
                    network = Network.DeStreamMain;

                DeStreamNodeSettings nodeSettings = new DeStreamNodeSettings(network, ProtocolVersion.ALT_PROTOCOL_VERSION, args: args, loadConfiguration: false);

                Console.WriteLine($"current network: {network.Name}");



                // NOTES: running BTC and STRAT side by side is not possible yet as the flags for serialization are static
                FullNode node = (FullNode)new FullNodeBuilder()
                    .UseNodeSettings(nodeSettings)
                    .UseBlockStore()
                    .UsePosConsensus()
                    .UseMempool()
                    .UseWallet()
                    .AddPowPosMining()
                    .UseApi()
                    .AddRPC()
                    .Build();

                //(Wallet wallet, Block block, ChainedHeader chainedHeader) result = TestClassHelper.CreateFirstTransaction(nodeSettings, node.WalletManager(), node.NodeService<WalletSettings>(),
                //    node.NodeService<IWalletFeePolicy>());
                //var walletManager = node.WalletManager();
                //walletManager.Wallets.Add(result.wallet);

                //HdAddress addr = result.wallet.AccountsRoot.ElementAt(0).Accounts.ElementAt(0).ExternalAddresses.ElementAt(0);
                //Key key = result.wallet.GetExtendedPrivateKeyForAddress("password", addr).PrivateKey;
                //TestClassHelper.CreateTestBlock(node, key);


                //walletManager.SaveWallets();

                //var test = node.WalletManager().GetSpendableTransactionsInWallet("myWallet1").Sum(s => s.Transaction.Amount);

                Process currentProcess = Process.GetCurrentProcess();
                Console.WriteLine("##############################: " + currentProcess.Id.ToString());
                int qwe0 = 1;
                if (node != null)
                    await node.RunAsync();
                int qwe = 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine("There was a problem initializing the node. Details: '{0}'", ex.Message);
            }
        }

    }
}

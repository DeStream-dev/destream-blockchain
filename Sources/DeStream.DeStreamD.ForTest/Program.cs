using System;
using System.Collections.Generic;
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

        #region Test
        public static DataFolder CreateDataFolder(object caller, [System.Runtime.CompilerServices.CallerMemberName] string callingMethod = "")
        {
            string directoryPath = GetTestDirectoryPath(caller, callingMethod);
            var dataFolder = new DataFolder(new NodeSettings(args: new string[] { $"-datadir={AssureEmptyDir(directoryPath)}" }).DataDir);
            return dataFolder;
        }
        public static string GetTestDirectoryPath(object caller, [System.Runtime.CompilerServices.CallerMemberName] string callingMethod = "")
        {
            return GetTestDirectoryPath(Path.Combine(caller.GetType().Name, callingMethod));
        }
        public static string AssureEmptyDir(string dir)
        {
            int deleteAttempts = 0;
            while (deleteAttempts < 50)
            {
                if (Directory.Exists(dir))
                {
                    try
                    {
                        Directory.Delete(dir, true);
                        break;
                    }
                    catch
                    {
                        deleteAttempts++;
                        Thread.Sleep(200);
                    }
                }
                else
                    break;
            }

            if (deleteAttempts >= 50)
                throw new Exception(string.Format("The test folder: {0} could not be deleted.", dir));

            Directory.CreateDirectory(dir);
            return dir;
        }
        #endregion

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

                var walletManager = node.WalletManager();
                

                Wallet wallet = TestClassHelper.CreateFirstTransaction(nodeSettings, walletManager);
                walletManager.Wallets.Add(wallet);
                //var x= node.WalletManager().CreateWallet("password", "MyWallet");
                //Wallet MyWallet = null;
                //try
                //{
                //    MyWallet = node.NodeService<IWalletManager>().LoadWallet("password", "MyWallet");
                //}
                //catch (Exception ex)
                //{
                //    node.WalletManager().CreateWallet("password", "MyWallet");
                //}
                //TestClassHelper.CreateFirstTransaction(MyWallet, nodeSettings);


                //node.NodeService<IWalletManager>().SaveWallet(MyWallet);

                //(ExtKey ExtKey, string ExtPubKey) accountKeys = TestClassHelper.GenerateAccountKeys(MyWallet, "password", "m/44'/0'/0'");
                //(PubKey PubKey, BitcoinPubKeyAddress Address) spendingKeys = TestClassHelper.GenerateAddressKeys(MyWallet, accountKeys.ExtPubKey, "0/0");
                //(PubKey PubKey, BitcoinPubKeyAddress Address) destinationKeys = TestClassHelper.GenerateAddressKeys(MyWallet, accountKeys.ExtPubKey, "0/1");
                //(PubKey PubKey, BitcoinPubKeyAddress Address) changeKeys = TestClassHelper.GenerateAddressKeys(MyWallet, accountKeys.ExtPubKey, "1/0");

                //var spendingAddress = new HdAddress
                //{
                //    Index = 0,
                //    HdPath = $"m/44'/0'/0'/0/0",
                //    Address = spendingKeys.Address.ToString(),
                //    Pubkey = spendingKeys.PubKey.ScriptPubKey,
                //    ScriptPubKey = spendingKeys.Address.ScriptPubKey,
                //    Transactions = new List<TransactionData>()
                //};
                //var destinationAddress = new HdAddress
                //{
                //    Index = 1,
                //    HdPath = $"m/44'/0'/0'/0/1",
                //    Address = destinationKeys.Address.ToString(),
                //    Pubkey = destinationKeys.PubKey.ScriptPubKey,
                //    ScriptPubKey = destinationKeys.Address.ScriptPubKey,
                //    Transactions = new List<TransactionData>()
                //};

                //var changeAddress = new HdAddress
                //{
                //    Index = 0,
                //    HdPath = $"m/44'/0'/0'/1/0",
                //    Address = changeKeys.Address.ToString(),
                //    Pubkey = changeKeys.PubKey.ScriptPubKey,
                //    ScriptPubKey = changeKeys.Address.ScriptPubKey,
                //    Transactions = new List<TransactionData>()
                //};

                //(ConcurrentChain chain, uint256 blockhash, Block block) chainInfo = TestClassHelper.CreateChainAndCreateFirstBlockWithPaymentToAddress(MyWallet.Network, spendingAddress);
                //var transaction = chainInfo.block.Transactions[0];
                //int blockHeight = chainInfo.chain.GetBlock(chainInfo.block.GetHash()).Height;
                //var walletManager = node.WalletManager();
                //walletManager.ProcessTransaction(transaction, blockHeight);
                //ChainedHeader chainedBlock = chainInfo.chain.GetBlock(chainInfo.block.GetHash());
                //walletManager.ProcessBlock(chainInfo.block, chainedBlock);
                int qwe = 1;
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

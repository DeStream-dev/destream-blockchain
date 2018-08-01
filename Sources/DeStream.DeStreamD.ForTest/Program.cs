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

                ////node.NodeService<IWalletManager>().LoadWallet("password", "mywallet123");

                //DataFolder dataFolder = CreateDataFolder(node);
                //Directory.CreateDirectory(dataFolder.WalletPath); 

                //var wallet = node.WalletManager().CreateWallet("password", "MyWallet");
                Wallet wallet = node.NodeService<IWalletManager>().LoadWallet("password", "MyWallet");
                (ExtKey ExtKey, string ExtPubKey) accountKeys = WalletTestsHelpers.GenerateAccountKeys(wallet, "password", "m/44'/0'/0'");
                (PubKey PubKey, BitcoinPubKeyAddress Address) spendingKeys = WalletTestsHelpers.GenerateAddressKeys(wallet, accountKeys.ExtPubKey, "0/0");
                (PubKey PubKey, BitcoinPubKeyAddress Address) destinationKeys = WalletTestsHelpers.GenerateAddressKeys(wallet, accountKeys.ExtPubKey, "0/1");
                (PubKey PubKey, BitcoinPubKeyAddress Address) changeKeys = WalletTestsHelpers.GenerateAddressKeys(wallet, accountKeys.ExtPubKey, "1/0");

                var spendingAddress = new HdAddress
                {
                    Index = 0,
                    HdPath = $"m/44'/0'/0'/0/0",
                    Address = spendingKeys.Address.ToString(),
                    Pubkey = spendingKeys.PubKey.ScriptPubKey,
                    ScriptPubKey = spendingKeys.Address.ScriptPubKey,
                    Transactions = new List<TransactionData>()
                };

                //var destinationAddress = new HdAddress
                //{
                //    Index = 1,
                //    HdPath = $"m/44'/0'/0'/0/1",
                //    Address = destinationKeys.Address.ToString(),
                //    Pubkey = destinationKeys.PubKey.ScriptPubKey,
                //    ScriptPubKey = destinationKeys.Address.ScriptPubKey,
                //    Transactions = new List<TransactionData>()
                //};

                var changeAddress = new HdAddress
                {
                    Index = 0,
                    HdPath = $"m/44'/0'/0'/1/0",
                    Address = changeKeys.Address.ToString(),
                    Pubkey = changeKeys.PubKey.ScriptPubKey,
                    ScriptPubKey = changeKeys.Address.ScriptPubKey,
                    Transactions = new List<TransactionData>()
                };

                //Generate a spendable transaction
                (ConcurrentChain chain, uint256 blockhash, Block block) chainInfo = WalletTestsHelpers.CreateChainAndCreateFirstBlockWithPaymentToAddress(wallet.Network, spendingAddress);
                /* CreateChainAndCreateFirstBlockWithPaymentToAddress */
                var chain = new ConcurrentChain(network);

                var _block = new Block();
                _block.Header.HashPrevBlock = chain.Tip.HashBlock;
                _block.Header.Bits = _block.Header.GetWorkRequired(network, chain.Tip);
                _block.Header.UpdateTime(DateTimeOffset.UtcNow, network, chain.Tip);

                var coinbase = new Transaction();
                coinbase.AddInput(TxIn.CreateCoinbase(chain.Height + 1));
                coinbase.AddOutput(new TxOut(network.GetReward(chain.Height + 1), spendingAddress.ScriptPubKey));

                _block.AddTransaction(coinbase);
                _block.Header.Nonce = 0;
                _block.UpdateMerkleRoot();
                _block.Header.PrecomputeHash();

                chain.SetTip(_block.Header);

                /* CreateChainAndCreateFirstBlockWithPaymentToAddress */

                //TransactionData spendingTransaction = WalletTestsHelpers.CreateTransactionDataFromFirstBlock(chainInfo);
                /*CreateTransactionDataFromFirstBlock*/
                //public static TransactionData CreateTransactionDataFromFirstBlock((ConcurrentChain chain, uint256 blockHash, Block block) chainInfo)
                //{
                    Transaction _transaction = _block.Transactions[0];

                    var addressTransaction = new TransactionData
                    {
                        Amount = _transaction.TotalOut,
                        BlockHash = _block.GetHash(),
                        BlockHeight = chainInfo.chain.GetBlock(_block.GetHash()).Height,
                        CreationTime = DateTimeOffset.FromUnixTimeSeconds(chainInfo.block.Header.Time),
                        Id = _transaction.GetHash(),
                        Index = 0,
                        ScriptPubKey = _transaction.Outputs[0].ScriptPubKey,
                    };
                    //return addressTransaction;
                //}

                /*CreateTransactionDataFromFirstBlock*/

                spendingAddress.Transactions.Add(addressTransaction);

                // setup a payment to yourself in a new block.
                //Transaction transaction = WalletTestsHelpers.SetupValidTransaction(wallet, "password", spendingAddress, destinationKeys.PubKey, changeAddress, new Money(7500), new Money(5000));
                #region SetupValidTransaction
                //Transaction SetupValidTransaction(Features.Wallet.Wallet wallet, string password, HdAddress spendingAddress, PubKey destinationPubKey, 
                //HdAddress changeAddress, Money amount, Money fee)
                TransactionData spendingTransaction = spendingAddress.Transactions.ElementAt(0);
                var coin = new Coin(spendingTransaction.Id, (uint)spendingTransaction.Index, spendingTransaction.Amount, spendingTransaction.ScriptPubKey);

                Key privateKey = Key.Parse(wallet.EncryptedSeed, "password", wallet.Network);
                var scriptPubKey = wallet.AccountsRoot.ElementAt(0).Accounts.ElementAt(0).ExternalAddresses.ElementAt(0).ScriptPubKey;
                
                var builder = new TransactionBuilder(wallet.Network);
                Transaction tx = builder
                    .AddCoins(new List<Coin> { coin })
                    .AddKeys(new ExtKey(privateKey, wallet.ChainCode).Derive(new KeyPath(spendingAddress.HdPath)).GetWif(wallet.Network))
                    .Send(scriptPubKey, new Money(7500))
                    .SetChange(changeAddress.ScriptPubKey)
                    .SendFees(new Money(5000))
                    .BuildTransaction(true);

                if (!builder.Verify(tx))
                {
                    throw new WalletException("Could not build transaction, please make sure you entered the correct data.");
                }


                #endregion
                //Block block = WalletTestsHelpers.AppendTransactionInNewBlockToChain(chainInfo.chain, transaction);
                Block block = WalletTestsHelpers.AppendTransactionInNewBlockToChain(chainInfo.chain, tx);
                HdAddress _addr = node.NodeService<IWalletManager>().GetUnusedAddress(new WalletAccountReference("MyWallet", "account 0"));
                wallet.AccountsRoot.ElementAt(0).Accounts.Add(new HdAccount
                {
                    Index = 0,
                    Name = "account1",
                    HdPath = "m/44'/0'/0'",
                    ExtendedPubKey = accountKeys.ExtPubKey,
                    ExternalAddresses = new List<HdAddress> { spendingAddress, _addr },
                    InternalAddresses = new List<HdAddress> { changeAddress }
                });

                var walletFeePolicy = new Mock<IWalletFeePolicy>();
                walletFeePolicy.Setup(w => w.GetMinimumFee(258, 50))
                    .Returns(new Money(5000));
                
                var walletManager = new WalletManager(nodeSettings.LoggerFactory, Network.DeStreamTest, chainInfo.chain, NodeSettings.Default(), new Mock<WalletSettings>().Object,
                    nodeSettings.DataFolder, walletFeePolicy.Object, new Mock<IAsyncLoopFactory>().Object, new NodeLifetime(), DateTimeProvider.Default);
                walletManager.Wallets.Add(wallet);
                walletManager.LoadKeysLookupLock();
                walletManager.WalletTipHash = block.Header.GetHash();

                ChainedHeader chainedBlock = chainInfo.chain.GetBlock(block.GetHash());
                walletManager.ProcessBlock(block, chainedBlock);

                HdAddress spentAddressResult = wallet.AccountsRoot.ElementAt(0).Accounts.ElementAt(0).ExternalAddresses.ElementAt(0);

                int qwe123 = 1;
                //HdAddress address = WalletTestsHelpers.CreateAddress(false);
                //this.walletManager.Setup(w => w.GetUnusedAddress(It.IsAny<WalletAccountReference>()))
                //    .Returns(address);

                //HdAddress _addr = node.NodeService<IWalletManager>().GetUnusedAddress(new WalletAccountReference("mywallet", "account 0"));
                //var _key0 = wallet.GetAllPubKeysByCoinType(CoinType.Stratis).FirstOrDefault();
                //Key _key = wallet.GetExtendedPrivateKeyForAddress("123456", _addr).PrivateKey;
                //var _walletTransactionHandler = ((FullNode)node).NodeService<IWalletTransactionHandler>() as WalletTransactionHandler;

                //var chain = new ConcurrentChain(_wallet.Network);
                //WalletTestsHelpers.AddBlocksWithCoinbaseToChain(_wallet.Network, chain, _addr);
                ////var walletAccountReference = new WalletAccountReference()
                //var account = _wallet.AccountsRoot.FirstOrDefault();
                //TransactionBuildContext context = CreateContext(new WalletAccountReference("mywallet", "account 0"), "123456", _key.PubKey.ScriptPubKey, new Money(777), FeeType.Low, 0);
                //Transaction transactionResult = _walletTransactionHandler.BuildTransaction(context);
                if (node != null)
                    await node.RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("There was a problem initializing the node. Details: '{0}'", ex.Message);
            }
        }

        public static TransactionBuildContext CreateContext(WalletAccountReference accountReference, string password,
            Script destinationScript, Money amount, FeeType feeType, int minConfirmations)
        {
            return new TransactionBuildContext(accountReference,
                new[] { new Recipient { Amount = amount, ScriptPubKey = destinationScript } }.ToList(), password)
            {
                MinConfirmations = minConfirmations,
                FeeType = feeType
            };
        }


    }
}

using System.Collections.Generic;
using System.IO;
using System.Linq;
using DeStream.Bitcoin.IntegrationTests.EnvironmentMockUpHelpers;
using Microsoft.Extensions.DependencyInjection;
using NBitcoin;
using DeStream.Bitcoin.Features.Consensus;
using DeStream.Bitcoin.Features.Wallet;
using DeStream.Bitcoin.Features.Wallet.Controllers;
using DeStream.Bitcoin.Features.Wallet.Interfaces;
using DeStream.Bitcoin.Features.Wallet.Models;
using Xunit;
using System;
using DeStream.Bitcoin.Utilities;
using System.Text;

namespace DeStream.Bitcoin.IntegrationTests
{
    public class WalletTests : IDisposable
    {
        private bool initialBlockSignature;
        public WalletTests()
        {
            this.initialBlockSignature = Block.BlockSignature;
            Block.BlockSignature = false;
        }

        public void Dispose()
        {
            Block.BlockSignature = this.initialBlockSignature;
        }

        [Fact]
        public void WalletCanReceiveAndSendCorrectly()
        {
            using (NodeBuilder builder = NodeBuilder.Create())
            {
                var destreamSender = builder.CreateDeStreamPowNode();
                var destreamReceiver = builder.CreateDeStreamPowNode();

                builder.StartAll();
                destreamSender.NotInIBD();
                destreamReceiver.NotInIBD();

                // get a key from the wallet
                var mnemonic1 = destreamSender.FullNode.WalletManager().CreateWallet("123456", "mywallet");
                var mnemonic2 = destreamReceiver.FullNode.WalletManager().CreateWallet("123456", "mywallet");
                Assert.Equal(12, mnemonic1.Words.Length);
                Assert.Equal(12, mnemonic2.Words.Length);
                var addr = destreamSender.FullNode.WalletManager().GetUnusedAddress(new WalletAccountReference("mywallet", "account 0"));
                var wallet = destreamSender.FullNode.WalletManager().GetWalletByName("mywallet");
                var key = wallet.GetExtendedPrivateKeyForAddress("123456", addr).PrivateKey;

                destreamSender.SetDummyMinerSecret(new BitcoinSecret(key, destreamSender.FullNode.Network));
                var maturity = (int)destreamSender.FullNode.Network.Consensus.Option<PowConsensusOptions>().CoinbaseMaturity;
                destreamSender.GenerateDeStream(maturity + 5);
                // wait for block repo for block sync to work

                TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(destreamSender));

                // the mining should add coins to the wallet
                var total = destreamSender.FullNode.WalletManager().GetSpendableTransactionsInWallet("mywallet").Sum(s => s.Transaction.Amount);
                Assert.Equal(Money.COIN * 105 * 50, total);

                // sync both nodes
                destreamSender.CreateRPCClient().AddNode(destreamReceiver.Endpoint, true);
                TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(destreamReceiver, destreamSender));

                // send coins to the receiver
                var sendto = destreamReceiver.FullNode.WalletManager().GetUnusedAddress(new WalletAccountReference("mywallet", "account 0"));
                var trx = destreamSender.FullNode.WalletTransactionHandler().BuildTransaction(CreateContext(
                    new WalletAccountReference("mywallet", "account 0"), "123456", sendto.ScriptPubKey, Money.COIN * 100, FeeType.Medium, 101));

                // broadcast to the other node
                destreamSender.FullNode.NodeService<WalletController>().SendTransaction(new SendTransactionRequest(trx.ToHex()));

                // wait for the trx to arrive
                TestHelper.WaitLoop(() => destreamReceiver.CreateRPCClient().GetRawMempool().Length > 0);
                TestHelper.WaitLoop(() => destreamReceiver.FullNode.WalletManager().GetSpendableTransactionsInWallet("mywallet").Any());

                var receivetotal = destreamReceiver.FullNode.WalletManager().GetSpendableTransactionsInWallet("mywallet").Sum(s => s.Transaction.Amount);
                Assert.Equal(Money.COIN * 100, receivetotal);
                Assert.Null(destreamReceiver.FullNode.WalletManager().GetSpendableTransactionsInWallet("mywallet").First().Transaction.BlockHeight);

                // generate two new blocks do the trx is confirmed
                destreamSender.GenerateDeStream(1, new List<Transaction>(new[] { trx.Clone() }));
                destreamSender.GenerateDeStream(1);

                // wait for block repo for block sync to work
                TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(destreamSender));
                TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(destreamReceiver, destreamSender));

                TestHelper.WaitLoop(() => maturity + 6 == destreamReceiver.FullNode.WalletManager().GetSpendableTransactionsInWallet("mywallet").First().Transaction.BlockHeight);
            }
        }

        [Fact]
        public void WalletCanSendOneTransactionWithManyOutputs()
        { 
            using (NodeBuilder builder = NodeBuilder.Create())
            {
                CoreNode destreamSender = builder.CreateDeStreamPowNode();
                CoreNode destreamReceiver = builder.CreateDeStreamPowNode();

                builder.StartAll();
                destreamSender.NotInIBD();
                destreamReceiver.NotInIBD();

                destreamSender.FullNode.WalletManager().CreateWallet("123456", "mywallet");
                destreamReceiver.FullNode.WalletManager().CreateWallet("123456", "mywallet");

                HdAddress addr = destreamSender.FullNode.WalletManager().GetUnusedAddress(new WalletAccountReference("mywallet", "account 0"));
                Wallet wallet = destreamSender.FullNode.WalletManager().GetWalletByName("mywallet");
                Key key = wallet.GetExtendedPrivateKeyForAddress("123456", addr).PrivateKey;

                destreamSender.SetDummyMinerSecret(new BitcoinSecret(key, destreamSender.FullNode.Network));
                int maturity = (int)destreamSender.FullNode.Network.Consensus.Option<PowConsensusOptions>().CoinbaseMaturity;
                destreamSender.GenerateDeStreamWithMiner(maturity + 51);

                // Wait for block repo for block sync to work.
                TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(destreamSender));
                
                Assert.Equal(Money.COIN * 150 * 50, destreamSender.FullNode.WalletManager().GetSpendableTransactionsInWallet("mywallet").Sum(s => s.Transaction.Amount));

                // Sync both nodes.
                destreamSender.CreateRPCClient().AddNode(destreamReceiver.Endpoint, true);
                TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(destreamReceiver, destreamSender));

                // Get 50 unused addresses from the receiver.
                IEnumerable<HdAddress> recevierAddresses = destreamReceiver.FullNode.WalletManager()
                    .GetUnusedAddresses(new WalletAccountReference("mywallet", "account 0"), 50);

                List<Recipient> recipients = recevierAddresses.Select(address => new Recipient
                    {
                        ScriptPubKey = address.ScriptPubKey,
                        Amount = Money.COIN
                    })
                    .ToList();

                var transactionBuildContext = new TransactionBuildContext(
                    new WalletAccountReference("mywallet", "account 0"), recipients, "123456")
                    {
                        FeeType = FeeType.Medium,
                        MinConfirmations = 101
                    };

                Transaction transaction = destreamSender.FullNode.WalletTransactionHandler().BuildTransaction(transactionBuildContext);
                Assert.Equal(51, transaction.Outputs.Count);

                // Broadcast to the other node.
                destreamSender.FullNode.NodeService<WalletController>().SendTransaction(new SendTransactionRequest(transaction.ToHex()));

                // Wait for the trx's to arrive.
                TestHelper.WaitLoop(() => destreamReceiver.CreateRPCClient().GetRawMempool().Length > 0);
                TestHelper.WaitLoop(() => destreamReceiver.FullNode.WalletManager().GetSpendableTransactionsInWallet("mywallet").Any());

                Assert.Equal(Money.COIN * 50, destreamReceiver.FullNode.WalletManager().GetSpendableTransactionsInWallet("mywallet").Sum(s => s.Transaction.Amount));
                Assert.Null(destreamReceiver.FullNode.WalletManager().GetSpendableTransactionsInWallet("mywallet").First().Transaction.BlockHeight);  

                // Generate new blocks so the trx is confirmed.
                destreamSender.GenerateDeStreamWithMiner(1);

                // Wait for block repo for block sync to work.
                TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(destreamSender));
                TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(destreamReceiver, destreamSender));

                // Confirm trx's have been committed to the block.
                Assert.Equal(maturity + 52 , destreamReceiver.FullNode.WalletManager().GetSpendableTransactionsInWallet("mywallet").First().Transaction.BlockHeight);
            }
        }

        [Fact]
        public void CanMineAndSendToAddress()
        {
            using (NodeBuilder builder = NodeBuilder.Create())
            {
                CoreNode destreamNodeSync = builder.CreateDeStreamPowNode();
                builder.StartAll();

                // Move a wallet file to the right folder and restart the wallet manager to take it into account.
                this.InitializeTestWallet(destreamNodeSync.FullNode.DataFolder.WalletPath);
                var walletManager = destreamNodeSync.FullNode.NodeService<IWalletManager>() as WalletManager;
                walletManager.Start();

                var rpc = destreamNodeSync.CreateRPCClient();
                rpc.SendCommand(NBitcoin.RPC.RPCOperations.generate, 10);
                Assert.Equal(10, rpc.GetBlockCount());

                var address = new Key().PubKey.GetAddress(rpc.Network);
                var tx = rpc.SendToAddress(address, Money.Coins(1.0m));
                Assert.NotNull(tx);
            }
        }

        [Fact]
        public void WalletCanReorg()
        {
            // this test has 4 parts:
            // send first transaction from one wallet to another and wait for it to be confirmed
            // send a second transaction and wait for it to be confirmed
            // connected to a longer chain that couse a reorg back so the second trasnaction is undone
            // mine the second transaction back in to the main chain

            using (NodeBuilder builder = NodeBuilder.Create())
            {
                var destreamSender = builder.CreateDeStreamPowNode();
                var destreamReceiver = builder.CreateDeStreamPowNode();
                var destreamReorg = builder.CreateDeStreamPowNode();

                builder.StartAll();
                destreamSender.NotInIBD();
                destreamReceiver.NotInIBD();
                destreamReorg.NotInIBD();

                // get a key from the wallet
                var mnemonic1 = destreamSender.FullNode.WalletManager().CreateWallet("123456", "mywallet");
                var mnemonic2 = destreamReceiver.FullNode.WalletManager().CreateWallet("123456", "mywallet");
                Assert.Equal(12, mnemonic1.Words.Length);
                Assert.Equal(12, mnemonic2.Words.Length);
                var addr = destreamSender.FullNode.WalletManager().GetUnusedAddress(new WalletAccountReference("mywallet", "account 0"));
                var wallet = destreamSender.FullNode.WalletManager().GetWalletByName("mywallet");
                var key = wallet.GetExtendedPrivateKeyForAddress("123456", addr).PrivateKey;

                destreamSender.SetDummyMinerSecret(new BitcoinSecret(key, destreamSender.FullNode.Network));
                destreamReorg.SetDummyMinerSecret(new BitcoinSecret(key, destreamSender.FullNode.Network));

                var maturity = (int)destreamSender.FullNode.Network.Consensus.Option<PowConsensusOptions>().CoinbaseMaturity;
                destreamSender.GenerateDeStreamWithMiner(maturity + 15);

                var currentBestHeight = maturity + 15;

                // wait for block repo for block sync to work
                TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(destreamSender));

                // the mining should add coins to the wallet
                var total = destreamSender.FullNode.WalletManager().GetSpendableTransactionsInWallet("mywallet").Sum(s => s.Transaction.Amount);
                Assert.Equal(Money.COIN * currentBestHeight * 50, total);

                // sync all nodes
                destreamReceiver.CreateRPCClient().AddNode(destreamSender.Endpoint, true);
                destreamReceiver.CreateRPCClient().AddNode(destreamReorg.Endpoint, true);
                destreamSender.CreateRPCClient().AddNode(destreamReorg.Endpoint, true);
                TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(destreamReceiver, destreamSender));
                TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(destreamReceiver, destreamReorg));

                // Build Transaction 1
                // ====================
                // send coins to the receiver
                var sendto = destreamReceiver.FullNode.WalletManager().GetUnusedAddress(new WalletAccountReference("mywallet", "account 0"));
                var transaction1 = destreamSender.FullNode.WalletTransactionHandler().BuildTransaction(CreateContext(new WalletAccountReference("mywallet", "account 0"), "123456", sendto.ScriptPubKey, Money.COIN * 100, FeeType.Medium, 101));

                // broadcast to the other node
                destreamSender.FullNode.NodeService<WalletController>().SendTransaction(new SendTransactionRequest(transaction1.ToHex()));

                // wait for the trx to arrive
                TestHelper.WaitLoop(() => destreamReceiver.CreateRPCClient().GetRawMempool().Length > 0);
                Assert.NotNull(destreamReceiver.CreateRPCClient().GetRawTransaction(transaction1.GetHash(), false));
                TestHelper.WaitLoop(() => destreamReceiver.FullNode.WalletManager().GetSpendableTransactionsInWallet("mywallet").Any());

                var receivetotal = destreamReceiver.FullNode.WalletManager().GetSpendableTransactionsInWallet("mywallet").Sum(s => s.Transaction.Amount);
                Assert.Equal(Money.COIN * 100, receivetotal);
                Assert.Null(destreamReceiver.FullNode.WalletManager().GetSpendableTransactionsInWallet("mywallet").First().Transaction.BlockHeight);

                // generate two new blocks so the trx is confirmed
                destreamSender.GenerateDeStreamWithMiner(1);
                var transaction1MinedHeight = currentBestHeight + 1;
                destreamSender.GenerateDeStreamWithMiner(1);
                currentBestHeight = currentBestHeight + 2;

                // wait for block repo for block sync to work
                TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(destreamSender));
                TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(destreamReceiver, destreamSender));
                TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(destreamReceiver, destreamReorg));
                Assert.Equal(currentBestHeight, destreamReceiver.FullNode.Chain.Tip.Height);
                TestHelper.WaitLoop(() => transaction1MinedHeight == destreamReceiver.FullNode.WalletManager().GetSpendableTransactionsInWallet("mywallet").First().Transaction.BlockHeight);

                // Build Transaction 2
                // ====================
                // remove the reorg node
                destreamReceiver.CreateRPCClient().RemoveNode(destreamReorg.Endpoint);
                destreamSender.CreateRPCClient().RemoveNode(destreamReorg.Endpoint);
                TestHelper.WaitLoop(() => !TestHelper.IsNodeConnected(destreamReorg));
                var forkblock = destreamReceiver.FullNode.Chain.Tip;

                // send more coins to the wallet
                sendto = destreamReceiver.FullNode.WalletManager().GetUnusedAddress(new WalletAccountReference("mywallet", "account 0"));
                var transaction2 = destreamSender.FullNode.WalletTransactionHandler().BuildTransaction(CreateContext(new WalletAccountReference("mywallet", "account 0"), "123456", sendto.ScriptPubKey, Money.COIN * 10, FeeType.Medium, 101));
                destreamSender.FullNode.NodeService<WalletController>().SendTransaction(new SendTransactionRequest(transaction2.ToHex()));
                // wait for the trx to arrive
                TestHelper.WaitLoop(() => destreamReceiver.CreateRPCClient().GetRawMempool().Length > 0);
                Assert.NotNull(destreamReceiver.CreateRPCClient().GetRawTransaction(transaction2.GetHash(), false));
                TestHelper.WaitLoop(() => destreamReceiver.FullNode.WalletManager().GetSpendableTransactionsInWallet("mywallet").Any());
                var newamount = destreamReceiver.FullNode.WalletManager().GetSpendableTransactionsInWallet("mywallet").Sum(s => s.Transaction.Amount);
                Assert.Equal(Money.COIN * 110, newamount);
                Assert.Contains(destreamReceiver.FullNode.WalletManager().GetSpendableTransactionsInWallet("mywallet"), b => b.Transaction.BlockHeight == null);

                // mine more blocks so its included in the chain

                destreamSender.GenerateDeStreamWithMiner(1);
                var transaction2MinedHeight = currentBestHeight + 1;
                destreamSender.GenerateDeStreamWithMiner(1);
                currentBestHeight = currentBestHeight + 2;
                TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(destreamSender));
                TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(destreamReceiver, destreamSender));
                Assert.Equal(currentBestHeight, destreamReceiver.FullNode.Chain.Tip.Height);
                TestHelper.WaitLoop(() => destreamReceiver.FullNode.WalletManager().GetSpendableTransactionsInWallet("mywallet").Any(b => b.Transaction.BlockHeight == transaction2MinedHeight));

                // create a reorg by mining on two different chains
                // ================================================
                // advance both chains, one chin is longer
                destreamSender.GenerateDeStreamWithMiner(2);
                destreamReorg.GenerateDeStreamWithMiner(10);
                currentBestHeight = forkblock.Height + 10;
                TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(destreamSender));
                TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(destreamReorg));

                // connect the reorg chain
                destreamReceiver.CreateRPCClient().AddNode(destreamReorg.Endpoint, true);
                destreamSender.CreateRPCClient().AddNode(destreamReorg.Endpoint, true);
                // wait for the chains to catch up
                TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(destreamReceiver, destreamSender));
                TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(destreamReceiver, destreamReorg));
                Assert.Equal(currentBestHeight, destreamReceiver.FullNode.Chain.Tip.Height);

                // ensure wallet reorg complete
                TestHelper.WaitLoop(() => destreamReceiver.FullNode.WalletManager().WalletTipHash == destreamReorg.CreateRPCClient().GetBestBlockHash());
                // check the wallet amount was rolled back
                var newtotal = destreamReceiver.FullNode.WalletManager().GetSpendableTransactionsInWallet("mywallet").Sum(s => s.Transaction.Amount);
                Assert.Equal(receivetotal, newtotal);
                TestHelper.WaitLoop(() => maturity + 16 == destreamReceiver.FullNode.WalletManager().GetSpendableTransactionsInWallet("mywallet").First().Transaction.BlockHeight);

                // ReBuild Transaction 2
                // ====================
                // After the reorg transaction2 was returned back to mempool
                destreamSender.FullNode.NodeService<WalletController>().SendTransaction(new SendTransactionRequest(transaction2.ToHex()));

                TestHelper.WaitLoop(() => destreamReceiver.CreateRPCClient().GetRawMempool().Length > 0);
                // mine the transaction again
                destreamSender.GenerateDeStreamWithMiner(1);
                transaction2MinedHeight = currentBestHeight + 1;
                destreamSender.GenerateDeStreamWithMiner(1);
                currentBestHeight = currentBestHeight + 2;

                TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(destreamSender));
                TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(destreamReceiver, destreamSender));
                TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(destreamReceiver, destreamReorg));
                Assert.Equal(currentBestHeight, destreamReceiver.FullNode.Chain.Tip.Height);
                var newsecondamount = destreamReceiver.FullNode.WalletManager().GetSpendableTransactionsInWallet("mywallet").Sum(s => s.Transaction.Amount);
                Assert.Equal(newamount, newsecondamount);
                TestHelper.WaitLoop(() => destreamReceiver.FullNode.WalletManager().GetSpendableTransactionsInWallet("mywallet").Any(b => b.Transaction.BlockHeight == transaction2MinedHeight));
            }
        }

        [Fact]
        public void Given__TheNodeHadAReorg_And_WalletTipIsBehindConsensusTip__When__ANewBlockArrives__Then__WalletCanRecover()
        {
            using (NodeBuilder builder = NodeBuilder.Create())
            {
                var destreamSender = builder.CreateDeStreamPowNode();
                var destreamReceiver = builder.CreateDeStreamPowNode();
                var destreamReorg = builder.CreateDeStreamPowNode();

                builder.StartAll();
                destreamSender.NotInIBD();
                destreamReceiver.NotInIBD();
                destreamReorg.NotInIBD();

                destreamSender.SetDummyMinerSecret(new BitcoinSecret(new Key(), destreamSender.FullNode.Network));
                destreamReorg.SetDummyMinerSecret(new BitcoinSecret(new Key(), destreamReorg.FullNode.Network));

                destreamSender.GenerateDeStreamWithMiner(10);

                // wait for block repo for block sync to work
                TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(destreamSender));

                //// sync all nodes
                destreamReceiver.CreateRPCClient().AddNode(destreamSender.Endpoint, true);
                destreamReceiver.CreateRPCClient().AddNode(destreamReorg.Endpoint, true);
                destreamSender.CreateRPCClient().AddNode(destreamReorg.Endpoint, true);
                TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(destreamReceiver, destreamSender));
                TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(destreamReceiver, destreamReorg));

                // remove the reorg node
                destreamReceiver.CreateRPCClient().RemoveNode(destreamReorg.Endpoint);
                destreamSender.CreateRPCClient().RemoveNode(destreamReorg.Endpoint);
                TestHelper.WaitLoop(() => !TestHelper.IsNodeConnected(destreamReorg));

                // create a reorg by mining on two different chains
                // ================================================
                // advance both chains, one chin is longer
                destreamSender.GenerateDeStreamWithMiner(2);
                destreamReorg.GenerateDeStreamWithMiner(10);
                TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(destreamSender));
                TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(destreamReorg));

                // rewind the wallet in the destreamReceiver node
                (destreamReceiver.FullNode.NodeService<IWalletSyncManager>() as WalletSyncManager).SyncFromHeight(5);

                // connect the reorg chain
                destreamReceiver.CreateRPCClient().AddNode(destreamReorg.Endpoint, true);
                destreamSender.CreateRPCClient().AddNode(destreamReorg.Endpoint, true);
                // wait for the chains to catch up
                TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(destreamReceiver, destreamSender));
                TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(destreamReceiver, destreamReorg));
                Assert.Equal(20, destreamReceiver.FullNode.Chain.Tip.Height);

                destreamSender.GenerateDeStreamWithMiner(5);

                TestHelper.TriggerSync(destreamReceiver);
                TestHelper.TriggerSync(destreamSender);
                TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(destreamReceiver, destreamSender));
                Assert.Equal(25, destreamReceiver.FullNode.Chain.Tip.Height);
            }
        }

        [Fact]
        public void Given__TheNodeHadAReorg_And_ConensusTipIsdifferentFromWalletTip__When__ANewBlockArrives__Then__WalletCanRecover()
        {
            using (NodeBuilder builder = NodeBuilder.Create())
            {
                var destreamSender = builder.CreateDeStreamPowNode();
                var destreamReceiver = builder.CreateDeStreamPowNode();
                var destreamReorg = builder.CreateDeStreamPowNode();

                builder.StartAll();
                destreamSender.NotInIBD();
                destreamReceiver.NotInIBD();
                destreamReorg.NotInIBD();

                destreamSender.SetDummyMinerSecret(new BitcoinSecret(new Key(), destreamSender.FullNode.Network));
                destreamReorg.SetDummyMinerSecret(new BitcoinSecret(new Key(), destreamReorg.FullNode.Network));

                destreamSender.GenerateDeStreamWithMiner(10);

                // wait for block repo for block sync to work
                TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(destreamSender));

                //// sync all nodes
                destreamReceiver.CreateRPCClient().AddNode(destreamSender.Endpoint, true);
                destreamReceiver.CreateRPCClient().AddNode(destreamReorg.Endpoint, true);
                destreamSender.CreateRPCClient().AddNode(destreamReorg.Endpoint, true);
                TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(destreamReceiver, destreamSender));
                TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(destreamReceiver, destreamReorg));

                // remove the reorg node and wait for node to be disconnected
                destreamReceiver.CreateRPCClient().RemoveNode(destreamReorg.Endpoint);
                destreamSender.CreateRPCClient().RemoveNode(destreamReorg.Endpoint);
                TestHelper.WaitLoop(() => !TestHelper.IsNodeConnected(destreamReorg));

                // create a reorg by mining on two different chains
                // ================================================
                // advance both chains, one chin is longer
                destreamSender.GenerateDeStreamWithMiner(2);
                destreamReorg.GenerateDeStreamWithMiner(10);
                TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(destreamSender));
                TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(destreamReorg));

                // connect the reorg chain
                destreamReceiver.CreateRPCClient().AddNode(destreamReorg.Endpoint, true);
                destreamSender.CreateRPCClient().AddNode(destreamReorg.Endpoint, true);
                // wait for the chains to catch up
                TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(destreamReceiver, destreamSender));
                TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(destreamReceiver, destreamReorg));
                Assert.Equal(20, destreamReceiver.FullNode.Chain.Tip.Height);

                // rewind the wallet in the destreamReceiver node
                (destreamReceiver.FullNode.NodeService<IWalletSyncManager>() as WalletSyncManager).SyncFromHeight(10);

                destreamSender.GenerateDeStreamWithMiner(5);

                TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(destreamReceiver, destreamSender));
                Assert.Equal(25, destreamReceiver.FullNode.Chain.Tip.Height);
            }
        }

        [Fact]
        public void WalletCanCatchupWithBestChain()
        {
            using (NodeBuilder builder = NodeBuilder.Create())
            {
                var destreamminer = builder.CreateDeStreamPowNode();

                builder.StartAll();
                destreamminer.NotInIBD();

                // get a key from the wallet
                var mnemonic = destreamminer.FullNode.WalletManager().CreateWallet("123456", "mywallet");
                Assert.Equal(12, mnemonic.Words.Length);
                var addr = destreamminer.FullNode.WalletManager().GetUnusedAddress(new WalletAccountReference("mywallet", "account 0"));
                var wallet = destreamminer.FullNode.WalletManager().GetWalletByName("mywallet");
                var key = wallet.GetExtendedPrivateKeyForAddress("123456", addr).PrivateKey;

                destreamminer.SetDummyMinerSecret(key.GetBitcoinSecret(destreamminer.FullNode.Network));
                destreamminer.GenerateDeStream(10);
                // wait for block repo for block sync to work
                TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(destreamminer));

                // push the wallet back
                destreamminer.FullNode.Services.ServiceProvider.GetService<IWalletSyncManager>().SyncFromHeight(5);

                destreamminer.GenerateDeStream(5);

                TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(destreamminer));
            }
        }

        [Fact]
        public void WalletCanRecoverOnStartup()
        {
            using (NodeBuilder builder = NodeBuilder.Create())
            {
                var destreamNodeSync = builder.CreateDeStreamPowNode();
                builder.StartAll();
                destreamNodeSync.NotInIBD();

                // get a key from the wallet
                var mnemonic = destreamNodeSync.FullNode.WalletManager().CreateWallet("123456", "mywallet");
                Assert.Equal(12, mnemonic.Words.Length);
                var addr = destreamNodeSync.FullNode.WalletManager().GetUnusedAddress(new WalletAccountReference("mywallet", "account 0"));
                var wallet = destreamNodeSync.FullNode.WalletManager().GetWalletByName("mywallet");
                var key = wallet.GetExtendedPrivateKeyForAddress("123456", addr).PrivateKey;

                destreamNodeSync.SetDummyMinerSecret(key.GetBitcoinSecret(destreamNodeSync.FullNode.Network));
                destreamNodeSync.GenerateDeStream(10);
                TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(destreamNodeSync));

                // set the tip of best chain some blocks in the apst
                destreamNodeSync.FullNode.Chain.SetTip(destreamNodeSync.FullNode.Chain.GetBlock(destreamNodeSync.FullNode.Chain.Height - 5));

                // stop the node it will persist the chain with the reset tip
                destreamNodeSync.FullNode.Dispose();

                var newNodeInstance = builder.CloneDeStreamNode(destreamNodeSync);

                // load the node, this should hit the block store recover code
                newNodeInstance.Start();

                // check that store recovered to be the same as the best chain.
                Assert.Equal(newNodeInstance.FullNode.Chain.Tip.HashBlock, newNodeInstance.FullNode.WalletManager().WalletTipHash);
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

        /// <summary>
        /// Copies the test wallet into data folder for node if it isnt' already present.
        /// </summary>
        /// <param name="path">The path of the folder to move the wallet to.</param>
        private void InitializeTestWallet(string path)
        {
            string testWalletPath = Path.Combine(path, "test.wallet.json");
            if (!File.Exists(testWalletPath))
                File.Copy("Data/test.wallet.json", testWalletPath);
        }
    }
}
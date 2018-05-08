using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Logging;
using NBitcoin;
using DeStream.Bitcoin.Connection;
using DeStream.Bitcoin.Features.BlockStore;
using DeStream.Bitcoin.IntegrationTests.EnvironmentMockUpHelpers;
using DeStream.Bitcoin.Utilities;
using Xunit;

namespace DeStream.Bitcoin.IntegrationTests
{
    public class BlockStoreTests
    {
        /// <summary>Factory for creating loggers.</summary>
        protected readonly ILoggerFactory loggerFactory;

        /// <summary>
        /// Initializes logger factory for tests in this class.
        /// </summary>
        public BlockStoreTests()
        {
            // These tests use Network.Main.
            // Ensure that these static flags have the expected values.
            Block.BlockSignature = false;
            Transaction.TimeStamp = false;

            this.loggerFactory = new LoggerFactory();
            DBreezeSerializer serializer = new DBreezeSerializer();
            serializer.Initialize();
        }

        private void BlockRepositoryBench()
        {
            using (var dir = TestDirectory.Create())
            {
                using (var blockRepo = new BlockRepository(Network.Main, dir.FolderName, DateTimeProvider.Default, this.loggerFactory))
                {
                    var lst = new List<Block>();
                    for (int i = 0; i < 30; i++)
                    {
                        // roughly 1mb blocks
                        var block = new Block();
                        for (int j = 0; j < 3000; j++)
                        {
                            var trx = new Transaction();
                            block.AddTransaction(new Transaction());
                            trx.AddInput(new TxIn(Script.Empty));
                            trx.AddOutput(Money.COIN + j + i, new Script(Guid.NewGuid().ToByteArray()
                                .Concat(Guid.NewGuid().ToByteArray())
                                .Concat(Guid.NewGuid().ToByteArray())
                                .Concat(Guid.NewGuid().ToByteArray())
                                .Concat(Guid.NewGuid().ToByteArray())
                                .Concat(Guid.NewGuid().ToByteArray())));
                            trx.AddInput(new TxIn(Script.Empty));
                            trx.AddOutput(Money.COIN + j + i + 1, new Script(Guid.NewGuid().ToByteArray()
                                .Concat(Guid.NewGuid().ToByteArray())
                                .Concat(Guid.NewGuid().ToByteArray())
                                .Concat(Guid.NewGuid().ToByteArray())
                                .Concat(Guid.NewGuid().ToByteArray())
                                .Concat(Guid.NewGuid().ToByteArray())));
                            block.AddTransaction(trx);
                        }
                        block.UpdateMerkleRoot();
                        block.Header.HashPrevBlock = lst.Any() ? lst.Last().GetHash() : Network.Main.GenesisHash;
                        lst.Add(block);
                    }

                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();
                    blockRepo.PutAsync(lst.Last().GetHash(), lst).GetAwaiter().GetResult();
                    var first = stopwatch.ElapsedMilliseconds;
                    blockRepo.PutAsync(lst.Last().GetHash(), lst).GetAwaiter().GetResult();
                    var second = stopwatch.ElapsedMilliseconds;
                }
            }
        }

        [Fact]
        public void BlockRepositoryPutBatch()
        {
            using (var dir = TestDirectory.Create())
            {
                using (var blockRepo = new BlockRepository(Network.Main, dir.FolderName, DateTimeProvider.Default, this.loggerFactory))
                {
                    blockRepo.SetTxIndexAsync(true).Wait();

                    var lst = new List<Block>();
                    for (int i = 0; i < 5; i++)
                    {
                        // put
                        var block = new Block();
                        block.AddTransaction(new Transaction());
                        block.AddTransaction(new Transaction());
                        block.Transactions[0].AddInput(new TxIn(Script.Empty));
                        block.Transactions[0].AddOutput(Money.COIN + i * 2, Script.Empty);
                        block.Transactions[1].AddInput(new TxIn(Script.Empty));
                        block.Transactions[1].AddOutput(Money.COIN + i * 2 + 1, Script.Empty);
                        block.UpdateMerkleRoot();
                        block.Header.HashPrevBlock = lst.Any() ? lst.Last().GetHash() : Network.Main.GenesisHash;
                        lst.Add(block);
                    }

                    blockRepo.PutAsync(lst.Last().GetHash(), lst).GetAwaiter().GetResult();

                    // check each block
                    foreach (var block in lst)
                    {
                        var received = blockRepo.GetAsync(block.GetHash()).GetAwaiter().GetResult();
                        Assert.True(block.ToBytes().SequenceEqual(received.ToBytes()));

                        foreach (var transaction in block.Transactions)
                        {
                            var trx = blockRepo.GetTrxAsync(transaction.GetHash()).GetAwaiter().GetResult();
                            Assert.True(trx.ToBytes().SequenceEqual(transaction.ToBytes()));
                        }
                    }

                    // delete
                    blockRepo.DeleteAsync(lst.ElementAt(2).GetHash(), new[] { lst.ElementAt(2).GetHash() }.ToList()).GetAwaiter().GetResult();
                    var deleted = blockRepo.GetAsync(lst.ElementAt(2).GetHash()).GetAwaiter().GetResult();
                    Assert.Null(deleted);
                }
            }
        }

        [Fact]
        public void BlockRepositoryBlockHash()
        {
            using (var dir = TestDirectory.Create())
            {
                using (var blockRepo = new BlockRepository(Network.Main, dir.FolderName, DateTimeProvider.Default, this.loggerFactory))
                {
                    blockRepo.InitializeAsync().GetAwaiter().GetResult();

                    Assert.Equal(Network.Main.GenesisHash, blockRepo.BlockHash);
                    var hash = new Block().GetHash();
                    blockRepo.SetBlockHashAsync(hash).GetAwaiter().GetResult();
                    Assert.Equal(hash, blockRepo.BlockHash);
                }
            }
        }

        [Fact]
        public void BlockBroadcastInv()
        {
            using (NodeBuilder builder = NodeBuilder.Create())
            {
                var destreamNodeSync = builder.CreateDeStreamPowNode();
                var destreamNode1 = builder.CreateDeStreamPowNode();
                var destreamNode2 = builder.CreateDeStreamPowNode();
                builder.StartAll();
                destreamNodeSync.NotInIBD();
                destreamNode1.NotInIBD();
                destreamNode2.NotInIBD();

                // generate blocks and wait for the downloader to pickup
                destreamNodeSync.SetDummyMinerSecret(new BitcoinSecret(new Key(), destreamNodeSync.FullNode.Network));
                destreamNodeSync.GenerateDeStreamWithMiner(10); // coinbase maturity = 10
                // wait for block repo for block sync to work
                TestHelper.WaitLoop(() => destreamNodeSync.FullNode.ConsensusLoop().Tip.HashBlock == destreamNodeSync.FullNode.Chain.Tip.HashBlock);
                TestHelper.WaitLoop(() => destreamNodeSync.FullNode.ChainBehaviorState.ConsensusTip.HashBlock == destreamNodeSync.FullNode.Chain.Tip.HashBlock);
                TestHelper.WaitLoop(() => destreamNodeSync.FullNode.HighestPersistedBlock().HashBlock == destreamNodeSync.FullNode.Chain.Tip.HashBlock);

                // sync both nodes
                destreamNode1.CreateRPCClient().AddNode(destreamNodeSync.Endpoint, true);
                destreamNode2.CreateRPCClient().AddNode(destreamNodeSync.Endpoint, true);
                TestHelper.WaitLoop(() => destreamNode1.CreateRPCClient().GetBestBlockHash() == destreamNodeSync.CreateRPCClient().GetBestBlockHash());
                TestHelper.WaitLoop(() => destreamNode2.CreateRPCClient().GetBestBlockHash() == destreamNodeSync.CreateRPCClient().GetBestBlockHash());

                // set node2 to use inv (not headers)
                destreamNode2.FullNode.ConnectionManager.ConnectedPeers.First().Behavior<BlockStoreBehavior>().PreferHeaders = false;

                // generate two new blocks
                destreamNodeSync.GenerateDeStreamWithMiner(2);
                // wait for block repo for block sync to work
                TestHelper.WaitLoop(() => destreamNodeSync.FullNode.Chain.Tip.HashBlock == destreamNodeSync.FullNode.ConsensusLoop().Tip.HashBlock);
                TestHelper.WaitLoop(() => destreamNodeSync.FullNode.BlockStoreManager().BlockRepository.GetAsync(destreamNodeSync.CreateRPCClient().GetBestBlockHash()).Result != null);

                // wait for the other nodes to pick up the newly generated blocks
                TestHelper.WaitLoop(() => destreamNode1.CreateRPCClient().GetBestBlockHash() == destreamNodeSync.CreateRPCClient().GetBestBlockHash());
                TestHelper.WaitLoop(() => destreamNode2.CreateRPCClient().GetBestBlockHash() == destreamNodeSync.CreateRPCClient().GetBestBlockHash());
            }
        }

        [Fact]
        public void BlockStoreCanRecoverOnStartup()
        {
            using (NodeBuilder builder = NodeBuilder.Create())
            {
                var destreamNodeSync = builder.CreateDeStreamPowNode();
                builder.StartAll();
                destreamNodeSync.NotInIBD();

                // generate blocks and wait for the downloader to pickup
                destreamNodeSync.SetDummyMinerSecret(new BitcoinSecret(new Key(), destreamNodeSync.FullNode.Network));

                destreamNodeSync.GenerateDeStreamWithMiner(10);
                TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(destreamNodeSync));

                // set the tip of best chain some blocks in the apst
                destreamNodeSync.FullNode.Chain.SetTip(destreamNodeSync.FullNode.Chain.GetBlock(destreamNodeSync.FullNode.Chain.Height - 5));

                // stop the node it will persist the chain with the reset tip
                destreamNodeSync.FullNode.Dispose();

                var newNodeInstance = builder.CloneDeStreamNode(destreamNodeSync);

                // load the node, this should hit the block store recover code
                newNodeInstance.Start();

                // check that store recovered to be the same as the best chain.
                Assert.Equal(newNodeInstance.FullNode.Chain.Tip.HashBlock, newNodeInstance.FullNode.HighestPersistedBlock().HashBlock);
                //TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(destreamNodeSync));
            }
        }

        [Fact]
        public void BlockStoreCanReorg()
        {
            using (NodeBuilder builder = NodeBuilder.Create())
            {
                var destreamNodeSync = builder.CreateDeStreamPowNode();
                var destreamNode1 = builder.CreateDeStreamPowNode();
                var destreamNode2 = builder.CreateDeStreamPowNode();
                builder.StartAll();
                destreamNodeSync.NotInIBD();
                destreamNode1.NotInIBD();
                destreamNode2.NotInIBD();

                // generate blocks and wait for the downloader to pickup
                destreamNode1.SetDummyMinerSecret(new BitcoinSecret(new Key(), destreamNodeSync.FullNode.Network));
                destreamNode2.SetDummyMinerSecret(new BitcoinSecret(new Key(), destreamNodeSync.FullNode.Network));
                // sync both nodes
                destreamNodeSync.CreateRPCClient().AddNode(destreamNode1.Endpoint, true);
                destreamNodeSync.CreateRPCClient().AddNode(destreamNode2.Endpoint, true);

                destreamNode1.GenerateDeStreamWithMiner(10);
                TestHelper.WaitLoop(() => destreamNode1.FullNode.HighestPersistedBlock().Height == 10);

                TestHelper.WaitLoop(() => destreamNode1.FullNode.HighestPersistedBlock().HashBlock == destreamNodeSync.FullNode.HighestPersistedBlock().HashBlock);
                TestHelper.WaitLoop(() => destreamNode2.FullNode.HighestPersistedBlock().HashBlock == destreamNodeSync.FullNode.HighestPersistedBlock().HashBlock);

                // remove node 2
                destreamNodeSync.CreateRPCClient().RemoveNode(destreamNode2.Endpoint);
                TestHelper.WaitLoop(() => !TestHelper.IsNodeConnected(destreamNode2));

                // mine some more with node 1
                destreamNode1.GenerateDeStreamWithMiner(10);

                // wait for node 1 to sync
                TestHelper.WaitLoop(() => destreamNode1.FullNode.HighestPersistedBlock().Height == 20);
                TestHelper.WaitLoop(() => destreamNode1.FullNode.HighestPersistedBlock().HashBlock == destreamNodeSync.FullNode.HighestPersistedBlock().HashBlock);

                // remove node 1
                destreamNodeSync.CreateRPCClient().RemoveNode(destreamNode1.Endpoint);
                TestHelper.WaitLoop(() => !TestHelper.IsNodeConnected(destreamNode1));

                // mine a higher chain with node2
                destreamNode2.GenerateDeStreamWithMiner(20);
                TestHelper.WaitLoop(() => destreamNode2.FullNode.HighestPersistedBlock().Height == 30);

                // add node2
                destreamNodeSync.CreateRPCClient().AddNode(destreamNode2.Endpoint, true);

                // node2 should be synced
                TestHelper.WaitLoop(() => destreamNode2.FullNode.HighestPersistedBlock().HashBlock == destreamNodeSync.FullNode.HighestPersistedBlock().HashBlock);
            }
        }

        [Fact]
        public void BlockStoreIndexTx()
        {
            using (NodeBuilder builder = NodeBuilder.Create())
            {
                var destreamNode1 = builder.CreateDeStreamPowNode();
                var destreamNode2 = builder.CreateDeStreamPowNode();
                builder.StartAll();
                destreamNode1.NotInIBD();
                destreamNode2.NotInIBD();

                // generate blocks and wait for the downloader to pickup
                destreamNode1.SetDummyMinerSecret(new BitcoinSecret(new Key(), destreamNode1.FullNode.Network));
                destreamNode2.SetDummyMinerSecret(new BitcoinSecret(new Key(), destreamNode2.FullNode.Network));
                // sync both nodes
                destreamNode1.CreateRPCClient().AddNode(destreamNode2.Endpoint, true);
                destreamNode1.GenerateDeStreamWithMiner(10);
                TestHelper.WaitLoop(() => destreamNode1.FullNode.HighestPersistedBlock().Height == 10);
                TestHelper.WaitLoop(() => destreamNode1.FullNode.HighestPersistedBlock().HashBlock == destreamNode2.FullNode.HighestPersistedBlock().HashBlock);

                var bestBlock1 = destreamNode1.FullNode.BlockStoreManager().BlockRepository.GetAsync(destreamNode1.FullNode.Chain.Tip.HashBlock).Result;
                Assert.NotNull(bestBlock1);

                // get the block coinbase trx
                var trx = destreamNode2.FullNode.BlockStoreManager().BlockRepository.GetTrxAsync(bestBlock1.Transactions.First().GetHash()).Result;
                Assert.NotNull(trx);
                Assert.Equal(bestBlock1.Transactions.First().GetHash(), trx.GetHash());
            }
        }
    }
}

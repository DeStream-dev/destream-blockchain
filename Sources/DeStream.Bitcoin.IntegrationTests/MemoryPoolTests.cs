using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NBitcoin;
using DeStream.Bitcoin.Features.Consensus;
using DeStream.Bitcoin.Features.MemoryPool;
using DeStream.Bitcoin.IntegrationTests.EnvironmentMockUpHelpers;
using DeStream.Bitcoin.Utilities;
using Xunit;

namespace DeStream.Bitcoin.IntegrationTests
{
    public class MemoryPoolTests
    {
        public class DateTimeProviderSet : DateTimeProvider
        {
            public long time;
            public DateTime timeutc;

            public override long GetTime()
            {
                return this.time;
            }

            public override DateTime GetUtcNow()
            {
                return this.timeutc;
            }
        }

        [Fact]
        public void AddToMempool()
        {
            using (NodeBuilder builder = NodeBuilder.Create())
            {
                var destreamNodeSync = builder.CreateDeStreamPowNode();
                builder.StartAll();

                destreamNodeSync.SetDummyMinerSecret(new BitcoinSecret(new Key(), destreamNodeSync.FullNode.Network));
                destreamNodeSync.GenerateDeStream(105); // coinbase maturity = 100
                TestHelper.WaitLoop(() => destreamNodeSync.FullNode.ConsensusLoop().Tip.HashBlock == destreamNodeSync.FullNode.Chain.Tip.HashBlock);
                TestHelper.WaitLoop(() => destreamNodeSync.FullNode.HighestPersistedBlock().HashBlock == destreamNodeSync.FullNode.Chain.Tip.HashBlock);

                var block = destreamNodeSync.FullNode.BlockStoreManager().BlockRepository.GetAsync(destreamNodeSync.FullNode.Chain.GetBlock(4).HashBlock).Result;
                var prevTrx = block.Transactions.First();
                var dest = new BitcoinSecret(new Key(), destreamNodeSync.FullNode.Network);

                Transaction tx = new Transaction();
                tx.AddInput(new TxIn(new OutPoint(prevTrx.GetHash(), 0), PayToPubkeyHashTemplate.Instance.GenerateScriptPubKey(destreamNodeSync.MinerSecret.PubKey)));
                tx.AddOutput(new TxOut("25", dest.PubKey.Hash));
                tx.AddOutput(new TxOut("24", new Key().PubKey.Hash)); // 1 btc fee
                tx.Sign(destreamNodeSync.MinerSecret, false);

                destreamNodeSync.Broadcast(tx);

                TestHelper.WaitLoop(() => destreamNodeSync.CreateRPCClient().GetRawMempool().Length == 1);
            }
        }

        [Fact]
        public void AddToMempoolTrxSpendingTwoOutputFromSameTrx()
        {
            using (NodeBuilder builder = NodeBuilder.Create())
            {
                var destreamNodeSync = builder.CreateDeStreamPowNode();
                builder.StartAll();
                destreamNodeSync.NotInIBD();

                destreamNodeSync.SetDummyMinerSecret(new BitcoinSecret(new Key(), destreamNodeSync.FullNode.Network));
                destreamNodeSync.GenerateDeStream(105); // coinbase maturity = 100
                TestHelper.WaitLoop(() => destreamNodeSync.FullNode.ConsensusLoop().Tip.HashBlock == destreamNodeSync.FullNode.Chain.Tip.HashBlock);
                TestHelper.WaitLoop(() => destreamNodeSync.FullNode.HighestPersistedBlock().HashBlock == destreamNodeSync.FullNode.Chain.Tip.HashBlock);

                var block = destreamNodeSync.FullNode.BlockStoreManager().BlockRepository.GetAsync(destreamNodeSync.FullNode.Chain.GetBlock(4).HashBlock).Result;
                var prevTrx = block.Transactions.First();
                var dest1 = new BitcoinSecret(new Key(), destreamNodeSync.FullNode.Network);
                var dest2 = new BitcoinSecret(new Key(), destreamNodeSync.FullNode.Network);

                Transaction parentTx = new Transaction();
                parentTx.AddInput(new TxIn(new OutPoint(prevTrx.GetHash(), 0), PayToPubkeyHashTemplate.Instance.GenerateScriptPubKey(destreamNodeSync.MinerSecret.PubKey)));
                parentTx.AddOutput(new TxOut("25", dest1.PubKey.Hash));
                parentTx.AddOutput(new TxOut("24", dest2.PubKey.Hash)); // 1 btc fee
                parentTx.Sign(destreamNodeSync.MinerSecret, false);
                destreamNodeSync.Broadcast(parentTx);
                // wiat for the trx to enter the pool
                TestHelper.WaitLoop(() => destreamNodeSync.CreateRPCClient().GetRawMempool().Length == 1);
                // mine the transactions in the mempool
                destreamNodeSync.GenerateDeStream(1, destreamNodeSync.FullNode.MempoolManager().InfoAllAsync().Result.Select(s => s.Trx).ToList());
                TestHelper.WaitLoop(() => destreamNodeSync.CreateRPCClient().GetRawMempool().Length == 0);

                //create a new trx spending both outputs
                Transaction tx = new Transaction();
                tx.AddInput(new TxIn(new OutPoint(parentTx.GetHash(), 0), PayToPubkeyHashTemplate.Instance.GenerateScriptPubKey(dest1.PubKey)));
                tx.AddInput(new TxIn(new OutPoint(parentTx.GetHash(), 1), PayToPubkeyHashTemplate.Instance.GenerateScriptPubKey(dest2.PubKey)));
                tx.AddOutput(new TxOut("48", new Key().PubKey.Hash)); // 1 btc fee
                var signed = new TransactionBuilder().AddKeys(dest1, dest2).AddCoins(parentTx.Outputs.AsCoins()).SignTransaction(tx);

                destreamNodeSync.Broadcast(signed);
                TestHelper.WaitLoop(() => destreamNodeSync.CreateRPCClient().GetRawMempool().Length == 1);
            }
        }

        [Fact]
        public void MempoolReceiveFromManyNodes()
        {
            using (NodeBuilder builder = NodeBuilder.Create())
            {
                var destreamNodeSync = builder.CreateDeStreamPowNode();
                builder.StartAll();
                destreamNodeSync.NotInIBD();

                destreamNodeSync.SetDummyMinerSecret(new BitcoinSecret(new Key(), destreamNodeSync.FullNode.Network));
                destreamNodeSync.GenerateDeStream(201); // coinbase maturity = 100
                TestHelper.WaitLoop(() => destreamNodeSync.FullNode.ConsensusLoop().Tip.HashBlock == destreamNodeSync.FullNode.Chain.Tip.HashBlock);
                TestHelper.WaitLoop(() => destreamNodeSync.FullNode.HighestPersistedBlock().HashBlock == destreamNodeSync.FullNode.Chain.Tip.HashBlock);

                var trxs = new List<Transaction>();
                foreach (var index in Enumerable.Range(1, 100))
                {
                    var block = destreamNodeSync.FullNode.BlockStoreManager().BlockRepository.GetAsync(destreamNodeSync.FullNode.Chain.GetBlock(index).HashBlock).Result;
                    var prevTrx = block.Transactions.First();
                    var dest = new BitcoinSecret(new Key(), destreamNodeSync.FullNode.Network);

                    Transaction tx = new Transaction();
                    tx.AddInput(new TxIn(new OutPoint(prevTrx.GetHash(), 0), PayToPubkeyHashTemplate.Instance.GenerateScriptPubKey(destreamNodeSync.MinerSecret.PubKey)));
                    tx.AddOutput(new TxOut("25", dest.PubKey.Hash));
                    tx.AddOutput(new TxOut("24", new Key().PubKey.Hash)); // 1 btc fee
                    tx.Sign(destreamNodeSync.MinerSecret, false);
                    trxs.Add(tx);
                }
                var options = new ParallelOptions { MaxDegreeOfParallelism = 10 };
                Parallel.ForEach(trxs, options, transaction =>
                {
                    destreamNodeSync.Broadcast(transaction);
                });

                TestHelper.WaitLoop(() => destreamNodeSync.CreateRPCClient().GetRawMempool().Length == 100);
            }
        }

        [Fact]
        public void TxMempoolBlockDoublespend()
        {
            using (NodeBuilder builder = NodeBuilder.Create())
            {
                var destreamNodeSync = builder.CreateDeStreamPowNode();
                builder.StartAll();
                destreamNodeSync.NotInIBD();
                destreamNodeSync.FullNode.Settings.RequireStandard = true; // make sure to test standard tx

                destreamNodeSync.SetDummyMinerSecret(new BitcoinSecret(new Key(), destreamNodeSync.FullNode.Network));
                destreamNodeSync.GenerateDeStream(100); // coinbase maturity = 100
                TestHelper.WaitLoop(() => destreamNodeSync.FullNode.ConsensusLoop().Tip.HashBlock == destreamNodeSync.FullNode.Chain.Tip.HashBlock);
                TestHelper.WaitLoop(() => destreamNodeSync.FullNode.HighestPersistedBlock().HashBlock == destreamNodeSync.FullNode.Chain.Tip.HashBlock);

                // Make sure skipping validation of transctions that were
                // validated going into the memory pool does not allow
                // double-spends in blocks to pass validation when they should not.

                var scriptPubKey = PayToPubkeyHashTemplate.Instance.GenerateScriptPubKey(destreamNodeSync.MinerSecret.PubKey);
                var genBlock = destreamNodeSync.FullNode.BlockStoreManager().BlockRepository.GetAsync(destreamNodeSync.FullNode.Chain.GetBlock(1).HashBlock).Result;

                // Create a double-spend of mature coinbase txn:
                List<Transaction> spends = new List<Transaction>(2);
                foreach (var index in Enumerable.Range(1, 2))
                {
                    var trx = new Transaction();
                    trx.AddInput(new TxIn(new OutPoint(genBlock.Transactions[0].GetHash(), 0), scriptPubKey));
                    trx.AddOutput(Money.Cents(11), new Key().PubKey.Hash);
                    // Sign:
                    trx.Sign(destreamNodeSync.MinerSecret, false);
                    spends.Add(trx);
                }

                // Test 1: block with both of those transactions should be rejected.
                var block = destreamNodeSync.GenerateDeStream(1, spends).Single();
                TestHelper.WaitLoop(() => destreamNodeSync.FullNode.ConsensusLoop().Tip.HashBlock == destreamNodeSync.FullNode.Chain.Tip.HashBlock);
                Assert.True(destreamNodeSync.FullNode.Chain.Tip.HashBlock != block.GetHash());

                // Test 2: ... and should be rejected if spend1 is in the memory pool
                Assert.True(destreamNodeSync.AddToDeStreamMempool(spends[0]));
                block = destreamNodeSync.GenerateDeStream(1, spends).Single();
                TestHelper.WaitLoop(() => destreamNodeSync.FullNode.ConsensusLoop().Tip.HashBlock == destreamNodeSync.FullNode.Chain.Tip.HashBlock);
                Assert.True(destreamNodeSync.FullNode.Chain.Tip.HashBlock != block.GetHash());
                destreamNodeSync.FullNode.MempoolManager().Clear().Wait();

                // Test 3: ... and should be rejected if spend2 is in the memory pool
                Assert.True(destreamNodeSync.AddToDeStreamMempool(spends[1]));
                block = destreamNodeSync.GenerateDeStream(1, spends).Single();
                TestHelper.WaitLoop(() => destreamNodeSync.FullNode.ConsensusLoop().Tip.HashBlock == destreamNodeSync.FullNode.Chain.Tip.HashBlock);
                Assert.True(destreamNodeSync.FullNode.Chain.Tip.HashBlock != block.GetHash());
                destreamNodeSync.FullNode.MempoolManager().Clear().Wait();

                // Final sanity test: first spend in mempool, second in block, that's OK:
                List<Transaction> oneSpend = new List<Transaction>();
                oneSpend.Add(spends[0]);
                Assert.True(destreamNodeSync.AddToDeStreamMempool(spends[1]));
                block = destreamNodeSync.GenerateDeStream(1, oneSpend).Single();
                TestHelper.WaitLoop(() => destreamNodeSync.FullNode.ConsensusLoop().Tip.HashBlock == destreamNodeSync.FullNode.Chain.Tip.HashBlock);
                Assert.True(destreamNodeSync.FullNode.Chain.Tip.HashBlock == block.GetHash());

                // spends[1] should have been removed from the mempool when the
                // block with spends[0] is accepted:
                TestHelper.WaitLoop(() => destreamNodeSync.FullNode.MempoolManager().MempoolSize().Result == 0);
            }
        }

        [Fact]
        public void TxMempoolMapOrphans()
        {
            var rand = new Random();
            var randByte = new byte[32];
            Func<uint256> randHash = () =>
            {
                rand.NextBytes(randByte);
                return new uint256(randByte);
            };

            using (NodeBuilder builder = NodeBuilder.Create())
            {
                var destreamNode = builder.CreateDeStreamPowNode();
                builder.StartAll();

                destreamNode.SetDummyMinerSecret(new BitcoinSecret(new Key(), destreamNode.FullNode.Network));

                // 50 orphan transactions:
                for (ulong i = 0; i < 50; i++)
                {
                    Transaction tx = new Transaction();
                    tx.AddInput(new TxIn(new OutPoint(randHash(), 0), new Script(OpcodeType.OP_1)));
                    tx.AddOutput(new TxOut(new Money(1 * Money.CENT), destreamNode.MinerSecret.ScriptPubKey));

                    destreamNode.FullNode.MempoolManager().Orphans.AddOrphanTx(i, tx).Wait();
                }

                Assert.Equal(50, destreamNode.FullNode.MempoolManager().Orphans.OrphansList().Count);

                // ... and 50 that depend on other orphans:
                for (ulong i = 0; i < 50; i++)
                {
                    var txPrev = destreamNode.FullNode.MempoolManager().Orphans.OrphansList().ElementAt(rand.Next(destreamNode.FullNode.MempoolManager().Orphans.OrphansList().Count));

                    Transaction tx = new Transaction();
                    tx.AddInput(new TxIn(new OutPoint(txPrev.Tx.GetHash(), 0), new Script(OpcodeType.OP_1)));
                    tx.AddOutput(new TxOut(new Money((1 + i + 100) * Money.CENT), destreamNode.MinerSecret.ScriptPubKey));
                    destreamNode.FullNode.MempoolManager().Orphans.AddOrphanTx(i, tx).Wait();
                }

                Assert.Equal(100, destreamNode.FullNode.MempoolManager().Orphans.OrphansList().Count);

                // This really-big orphan should be ignored:
                for (ulong i = 0; i < 10; i++)
                {
                    var txPrev = destreamNode.FullNode.MempoolManager().Orphans.OrphansList().ElementAt(rand.Next(destreamNode.FullNode.MempoolManager().Orphans.OrphansList().Count));
                    Transaction tx = new Transaction();
                    tx.AddOutput(new TxOut(new Money(1 * Money.CENT), destreamNode.MinerSecret.ScriptPubKey));
                    foreach (var index in Enumerable.Range(0, 2777))
                        tx.AddInput(new TxIn(new OutPoint(txPrev.Tx.GetHash(), index), new Script(OpcodeType.OP_1)));

                    Assert.False(destreamNode.FullNode.MempoolManager().Orphans.AddOrphanTx(i, tx).Result);
                }

                Assert.Equal(100, destreamNode.FullNode.MempoolManager().Orphans.OrphansList().Count);

                // Test EraseOrphansFor:
                for (ulong i = 0; i < 3; i++)
                {
                    var sizeBefore = destreamNode.FullNode.MempoolManager().Orphans.OrphansList().Count;
                    destreamNode.FullNode.MempoolManager().Orphans.EraseOrphansFor(i).Wait();
                    Assert.True(destreamNode.FullNode.MempoolManager().Orphans.OrphansList().Count < sizeBefore);
                }

                // Test LimitOrphanTxSize() function:
                destreamNode.FullNode.MempoolManager().Orphans.LimitOrphanTxSizeAsync(40).Wait();
                Assert.True(destreamNode.FullNode.MempoolManager().Orphans.OrphansList().Count <= 40);
                destreamNode.FullNode.MempoolManager().Orphans.LimitOrphanTxSizeAsync(10).Wait();
                Assert.True(destreamNode.FullNode.MempoolManager().Orphans.OrphansList().Count <= 10);
                destreamNode.FullNode.MempoolManager().Orphans.LimitOrphanTxSizeAsync(0).Wait();
                Assert.True(!destreamNode.FullNode.MempoolManager().Orphans.OrphansList().Any());
            }
        }

        [Fact]
        public void MempoolAddNodeWithOrphans()
        {
            using (NodeBuilder builder = NodeBuilder.Create())
            {
                var destreamNodeSync = builder.CreateDeStreamPowNode();
                builder.StartAll();
                destreamNodeSync.NotInIBD();

                destreamNodeSync.SetDummyMinerSecret(new BitcoinSecret(new Key(), destreamNodeSync.FullNode.Network));
                destreamNodeSync.GenerateDeStream(101); // coinbase maturity = 100
                TestHelper.WaitLoop(() => destreamNodeSync.FullNode.ConsensusLoop().Tip.HashBlock == destreamNodeSync.FullNode.Chain.Tip.HashBlock);
                TestHelper.WaitLoop(() => destreamNodeSync.FullNode.ChainBehaviorState.ConsensusTip.HashBlock == destreamNodeSync.FullNode.Chain.Tip.HashBlock);
                TestHelper.WaitLoop(() => destreamNodeSync.FullNode.HighestPersistedBlock().HashBlock == destreamNodeSync.FullNode.Chain.Tip.HashBlock);

                var block = destreamNodeSync.FullNode.BlockStoreManager().BlockRepository.GetAsync(destreamNodeSync.FullNode.Chain.GetBlock(1).HashBlock).Result;
                var prevTrx = block.Transactions.First();
                var dest = new BitcoinSecret(new Key(), destreamNodeSync.FullNode.Network);

                var key = new Key();
                Transaction tx = new Transaction();
                tx.AddInput(new TxIn(new OutPoint(prevTrx.GetHash(), 0), PayToPubkeyHashTemplate.Instance.GenerateScriptPubKey(destreamNodeSync.MinerSecret.PubKey)));
                tx.AddOutput(new TxOut("25", dest.PubKey.Hash));
                tx.AddOutput(new TxOut("24", key.PubKey.Hash)); // 1 btc fee
                tx.Sign(destreamNodeSync.MinerSecret, false);

                Transaction txOrphan = new Transaction();
                txOrphan.AddInput(new TxIn(new OutPoint(tx.GetHash(), 1), PayToPubkeyHashTemplate.Instance.GenerateScriptPubKey(key.PubKey)));
                txOrphan.AddOutput(new TxOut("10", new Key().PubKey.Hash));
                txOrphan.Sign(key.GetBitcoinSecret(destreamNodeSync.FullNode.Network), false);

                // broadcast the orphan
                destreamNodeSync.Broadcast(txOrphan);
                TestHelper.WaitLoop(() => destreamNodeSync.FullNode.MempoolManager().Orphans.OrphansList().Count == 1);
                // broadcast the parent
                destreamNodeSync.Broadcast(tx);
                TestHelper.WaitLoop(() => destreamNodeSync.FullNode.MempoolManager().Orphans.OrphansList().Count == 0);
                // wait for orphan to get in the pool
                TestHelper.WaitLoop(() => destreamNodeSync.CreateRPCClient().GetRawMempool().Length == 2);
            }
        }

        [Fact]
        public void MempoolSyncTransactions()
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
                destreamNodeSync.GenerateDeStreamWithMiner(105); // coinbase maturity = 100
                // wait for block repo for block sync to work
                TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(destreamNodeSync));

                // sync both nodes
                destreamNode1.CreateRPCClient().AddNode(destreamNodeSync.Endpoint, true);
                destreamNode2.CreateRPCClient().AddNode(destreamNodeSync.Endpoint, true);
                TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(destreamNode1, destreamNodeSync));
                TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(destreamNode2, destreamNodeSync));

                // create some transactions and push them to the pool
                var trxs = new List<Transaction>();
                foreach (var index in Enumerable.Range(1, 5))
                {
                    var block = destreamNodeSync.FullNode.BlockStoreManager().BlockRepository.GetAsync(destreamNodeSync.FullNode.Chain.GetBlock(index).HashBlock).Result;
                    var prevTrx = block.Transactions.First();
                    var dest = new BitcoinSecret(new Key(), destreamNodeSync.FullNode.Network);

                    Transaction tx = new Transaction();
                    tx.AddInput(new TxIn(new OutPoint(prevTrx.GetHash(), 0), PayToPubkeyHashTemplate.Instance.GenerateScriptPubKey(destreamNodeSync.MinerSecret.PubKey)));
                    tx.AddOutput(new TxOut("25", dest.PubKey.Hash));
                    tx.AddOutput(new TxOut("24", new Key().PubKey.Hash)); // 1 btc fee
                    tx.Sign(destreamNodeSync.MinerSecret, false);
                    trxs.Add(tx);
                }
                var options = new ParallelOptions { MaxDegreeOfParallelism = 5 };
                Parallel.ForEach(trxs, options, transaction =>
                {
                    destreamNodeSync.Broadcast(transaction);
                });

                // wait for all nodes to have all trx
                TestHelper.WaitLoop(() => destreamNodeSync.CreateRPCClient().GetRawMempool().Length == 5);

                // the full node should be connected to both nodes
                Assert.True(destreamNodeSync.FullNode.ConnectionManager.ConnectedPeers.Count() >= 2);

                // reset the trickle timer on the full node that has the transactions in the pool
                foreach (var node in destreamNodeSync.FullNode.ConnectionManager.ConnectedPeers) node.Behavior<MempoolBehavior>().NextInvSend = 0;

                TestHelper.WaitLoop(() => destreamNode1.CreateRPCClient().GetRawMempool().Length == 5);
                TestHelper.WaitLoop(() => destreamNode2.CreateRPCClient().GetRawMempool().Length == 5);

                // mine the transactions in the mempool
                destreamNodeSync.GenerateDeStreamWithMiner(1);
                TestHelper.WaitLoop(() => destreamNodeSync.CreateRPCClient().GetRawMempool().Length == 0);

                // wait for block and mempool to change
                TestHelper.WaitLoop(() => destreamNode1.CreateRPCClient().GetBestBlockHash() == destreamNodeSync.CreateRPCClient().GetBestBlockHash());
                TestHelper.WaitLoop(() => destreamNode2.CreateRPCClient().GetBestBlockHash() == destreamNodeSync.CreateRPCClient().GetBestBlockHash());
                TestHelper.WaitLoop(() => destreamNode1.CreateRPCClient().GetRawMempool().Length == 0);
                TestHelper.WaitLoop(() => destreamNode2.CreateRPCClient().GetRawMempool().Length == 0);
            }
        }
    }

    public class TestMemPoolEntryHelper
    {
        // Default values
        private Money nFee = Money.Zero;

        private long nTime = 0;
        private double dPriority = 0.0;
        private int nHeight = 1;
        private bool spendsCoinbase = false;
        private long sigOpCost = 4;
        private LockPoints lp = new LockPoints();

        public TxMempoolEntry FromTx(Transaction tx, TxMempool pool = null)
        {
            Money inChainValue = (pool != null && pool.HasNoInputsOf(tx)) ? tx.TotalOut : 0;

            return new TxMempoolEntry(tx, this.nFee, this.nTime, this.dPriority, this.nHeight,
                inChainValue, this.spendsCoinbase, this.sigOpCost, this.lp, new PowConsensusOptions());
        }

        // Change the default value
        public TestMemPoolEntryHelper Fee(Money fee) { this.nFee = fee; return this; }

        public TestMemPoolEntryHelper Time(long time)
        {
            this.nTime = time; return this;
        }

        public TestMemPoolEntryHelper Priority(double priority)
        {
            this.dPriority = priority; return this;
        }

        public TestMemPoolEntryHelper Height(int height)
        {
            this.nHeight = height; return this;
        }

        public TestMemPoolEntryHelper SpendsCoinbase(bool flag)
        {
            this.spendsCoinbase = flag; return this;
        }

        public TestMemPoolEntryHelper SigOpsCost(long sigopsCost)
        {
            this.sigOpCost = sigopsCost; return this;
        }
    }
}

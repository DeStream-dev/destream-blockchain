using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using DeStream.Bitcoin.Connection;
using DeStream.Bitcoin.IntegrationTests.EnvironmentMockUpHelpers;
using Xunit;

namespace DeStream.Bitcoin.IntegrationTests
{
    public class NodeSyncTests
    {
        public NodeSyncTests()
        {
            // These tests are for mostly for POW. Set the flags to the expected values.
            Transaction.TimeStamp = false;
            Block.BlockSignature = false;
        }

        [Fact]
        public void NodesCanConnectToEachOthers()
        {
            using (NodeBuilder builder = NodeBuilder.Create())
            {
                var node1 = builder.CreateDeStreamPowNode();
                var node2 = builder.CreateDeStreamPowNode();
                builder.StartAll();
                Assert.Empty(node1.FullNode.ConnectionManager.ConnectedPeers);
                Assert.Empty(node2.FullNode.ConnectionManager.ConnectedPeers);
                var rpc1 = node1.CreateRPCClient();
                rpc1.AddNode(node2.Endpoint, true);
                Assert.Single(node1.FullNode.ConnectionManager.ConnectedPeers);
                Assert.Single(node2.FullNode.ConnectionManager.ConnectedPeers);

                var behavior = node1.FullNode.ConnectionManager.ConnectedPeers.First().Behaviors.Find<ConnectionManagerBehavior>();
                Assert.False(behavior.Inbound);
                Assert.True(behavior.OneTry);
                behavior = node2.FullNode.ConnectionManager.ConnectedPeers.First().Behaviors.Find<ConnectionManagerBehavior>();
                Assert.True(behavior.Inbound);
                Assert.False(behavior.OneTry);
            }
        }

        [Fact]
        public void CanDeStreamSyncFromCore()
        {
            using (NodeBuilder builder = NodeBuilder.Create())
            {
                var destreamNode = builder.CreateDeStreamPowNode();
                var coreNode = builder.CreateNode();
                builder.StartAll();

                destreamNode.NotInIBD();

                var tip = coreNode.FindBlock(10).Last();
                destreamNode.CreateRPCClient().AddNode(coreNode.Endpoint, true);
                TestHelper.WaitLoop(() => destreamNode.CreateRPCClient().GetBestBlockHash() == coreNode.CreateRPCClient().GetBestBlockHash());
                var bestBlockHash = destreamNode.CreateRPCClient().GetBestBlockHash();
                Assert.Equal(tip.GetHash(), bestBlockHash);

                //Now check if Core connect to destream
                destreamNode.CreateRPCClient().RemoveNode(coreNode.Endpoint);
                TestHelper.WaitLoop(() => coreNode.CreateRPCClient().GetPeersInfo().Length == 0);

                tip = coreNode.FindBlock(10).Last();
                coreNode.CreateRPCClient().AddNode(destreamNode.Endpoint, true);
                TestHelper.WaitLoop(() => destreamNode.CreateRPCClient().GetBestBlockHash() == coreNode.CreateRPCClient().GetBestBlockHash());
                bestBlockHash = destreamNode.CreateRPCClient().GetBestBlockHash();
                Assert.Equal(tip.GetHash(), bestBlockHash);
            }
        }

        [Fact]
        public void CanDeStreamSyncFromDeStream()
        {
            using (NodeBuilder builder = NodeBuilder.Create())
            {
                var destreamNode = builder.CreateDeStreamPowNode();
                var destreamNodeSync = builder.CreateDeStreamPowNode();
                var coreCreateNode = builder.CreateNode();
                builder.StartAll();

                destreamNode.NotInIBD();
                destreamNodeSync.NotInIBD();

                // first seed a core node with blocks and sync them to a destream node
                // and wait till the destream node is fully synced
                var tip = coreCreateNode.FindBlock(5).Last();
                destreamNode.CreateRPCClient().AddNode(coreCreateNode.Endpoint, true);
                TestHelper.WaitLoop(() => destreamNode.CreateRPCClient().GetBestBlockHash() == coreCreateNode.CreateRPCClient().GetBestBlockHash());
                var bestBlockHash = destreamNode.CreateRPCClient().GetBestBlockHash();
                Assert.Equal(tip.GetHash(), bestBlockHash);

                // add a new destream node which will download
                // the blocks using the GetData payload
                destreamNodeSync.CreateRPCClient().AddNode(destreamNode.Endpoint, true);

                // wait for download and assert
                TestHelper.WaitLoop(() => destreamNode.CreateRPCClient().GetBestBlockHash() == destreamNodeSync.CreateRPCClient().GetBestBlockHash());
                bestBlockHash = destreamNodeSync.CreateRPCClient().GetBestBlockHash();
                Assert.Equal(tip.GetHash(), bestBlockHash);
            }
        }

        [Fact]
        public void CanCoreSyncFromDeStream()
        {
            using (NodeBuilder builder = NodeBuilder.Create())
            {
                var destreamNode = builder.CreateDeStreamPowNode();
                var coreNodeSync = builder.CreateNode();
                var coreCreateNode = builder.CreateNode();
                builder.StartAll();

                destreamNode.NotInIBD();

                // first seed a core node with blocks and sync them to a destream node
                // and wait till the destream node is fully synced
                var tip = coreCreateNode.FindBlock(5).Last();
                destreamNode.CreateRPCClient().AddNode(coreCreateNode.Endpoint, true);
                TestHelper.WaitLoop(() => destreamNode.CreateRPCClient().GetBestBlockHash() == coreCreateNode.CreateRPCClient().GetBestBlockHash());
                TestHelper.WaitLoop(() => destreamNode.FullNode.HighestPersistedBlock().HashBlock == destreamNode.FullNode.Chain.Tip.HashBlock);

                var bestBlockHash = destreamNode.CreateRPCClient().GetBestBlockHash();
                Assert.Equal(tip.GetHash(), bestBlockHash);

                // add a new destream node which will download
                // the blocks using the GetData payload
                coreNodeSync.CreateRPCClient().AddNode(destreamNode.Endpoint, true);

                // wait for download and assert
                TestHelper.WaitLoop(() => destreamNode.CreateRPCClient().GetBestBlockHash() == coreNodeSync.CreateRPCClient().GetBestBlockHash());
                bestBlockHash = coreNodeSync.CreateRPCClient().GetBestBlockHash();
                Assert.Equal(tip.GetHash(), bestBlockHash);
            }
        }

        [Fact]
        public void Given__NodesAreSynced__When__ABigReorgHappens__Then__TheReorgIsIgnored()
        {
            // Temporary fix so the Network static initialize will not break.
            var m = Network.Main;
            Transaction.TimeStamp = true;
            Block.BlockSignature = true;
            try
            {
                using (NodeBuilder builder = NodeBuilder.Create())
                {
                    var destreamMiner = builder.CreateDeStreamPosNode();
                    var destreamSyncer = builder.CreateDeStreamPosNode();
                    var destreamReorg = builder.CreateDeStreamPosNode();

                    builder.StartAll();
                    destreamMiner.NotInIBD();
                    destreamSyncer.NotInIBD();
                    destreamReorg.NotInIBD();

                    // TODO: set the max allowed reorg threshold here
                    // assume a reorg of 10 blocks is not allowed.
                    destreamMiner.FullNode.ChainBehaviorState.MaxReorgLength = 10;
                    destreamSyncer.FullNode.ChainBehaviorState.MaxReorgLength = 10;
                    destreamReorg.FullNode.ChainBehaviorState.MaxReorgLength = 10;

                    destreamMiner.SetDummyMinerSecret(new BitcoinSecret(new Key(), destreamMiner.FullNode.Network));
                    destreamReorg.SetDummyMinerSecret(new BitcoinSecret(new Key(), destreamReorg.FullNode.Network));

                    destreamMiner.GenerateDeStreamWithMiner(1);

                    // wait for block repo for block sync to work
                    TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(destreamMiner));
                    destreamMiner.CreateRPCClient().AddNode(destreamReorg.Endpoint, true);
                    destreamMiner.CreateRPCClient().AddNode(destreamSyncer.Endpoint, true);

                    TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(destreamMiner, destreamSyncer));
                    TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(destreamMiner, destreamReorg));

                    // create a reorg by mining on two different chains
                    // ================================================

                    destreamMiner.CreateRPCClient().RemoveNode(destreamReorg.Endpoint);
                    destreamSyncer.CreateRPCClient().RemoveNode(destreamReorg.Endpoint);
                    TestHelper.WaitLoop(() => !TestHelper.IsNodeConnected(destreamReorg));

                    var t1 = Task.Run(() => destreamMiner.GenerateDeStreamWithMiner(11));
                    var t2 = Task.Delay(1000).ContinueWith(t => destreamReorg.GenerateDeStreamWithMiner(12));
                    Task.WaitAll(t1, t2);
                    TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(destreamMiner));
                    TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(destreamReorg));

                    // make sure the nodes are actually on different chains.
                    Assert.NotEqual(destreamMiner.FullNode.Chain.GetBlock(2).HashBlock, destreamReorg.FullNode.Chain.GetBlock(2).HashBlock);

                    TestHelper.TriggerSync(destreamSyncer);
                    TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(destreamMiner, destreamSyncer));

                    // The hash before the reorg node is connected.
                    var hashBeforeReorg = destreamMiner.FullNode.Chain.Tip.HashBlock;

                    // connect the reorg chain
                    destreamMiner.CreateRPCClient().AddNode(destreamReorg.Endpoint, true);
                    destreamSyncer.CreateRPCClient().AddNode(destreamReorg.Endpoint, true);

                    // trigger nodes to sync
                    TestHelper.TriggerSync(destreamMiner);
                    TestHelper.TriggerSync(destreamReorg);
                    TestHelper.TriggerSync(destreamSyncer);

                    // wait for the synced chain to get headers updated.
                    TestHelper.WaitLoop(() => !destreamReorg.FullNode.ConnectionManager.ConnectedPeers.Any());

                    TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(destreamMiner, destreamSyncer));
                    TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(destreamReorg, destreamMiner) == false);
                    TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(destreamReorg, destreamSyncer) == false);

                    // check that a reorg did not happen.
                    Assert.Equal(hashBeforeReorg, destreamSyncer.FullNode.Chain.Tip.HashBlock);
                }
            }
            finally
            {
                Transaction.TimeStamp = false;
                Block.BlockSignature = false;
            }
        }

        /// <summary>
        /// This tests simulates scenario 2 from issue 636.
        /// <para>
        /// The test mines a block and roughly at the same time, but just after that, a new block at the same height
        /// arrives from the puller. Then another block comes from the puller extending the chain without the block we mined.
        /// </para>
        /// </summary>
        [Fact]
        public void PullerVsMinerRaceCondition()
        {
            // Temporary fix so the Network static initialize will not break.
            var m = Network.Main;
            Transaction.TimeStamp = true;
            Block.BlockSignature = true;
            try
            {
                using (NodeBuilder builder = NodeBuilder.Create())
                {
                    // This represents local node.
                    var destreamMinerLocal = builder.CreateDeStreamPosNode();

                    // This represents remote, which blocks are received by local node using its puller.
                    var destreamMinerRemote = builder.CreateDeStreamPosNode();

                    builder.StartAll();
                    destreamMinerLocal.NotInIBD();
                    destreamMinerRemote.NotInIBD();

                    destreamMinerLocal.SetDummyMinerSecret(new BitcoinSecret(new Key(), destreamMinerLocal.FullNode.Network));
                    destreamMinerRemote.SetDummyMinerSecret(new BitcoinSecret(new Key(), destreamMinerRemote.FullNode.Network));

                    // Let's mine block Ap and Bp.
                    destreamMinerRemote.GenerateDeStreamWithMiner(2);

                    // Wait for block repository for block sync to work.
                    TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(destreamMinerRemote));
                    destreamMinerLocal.CreateRPCClient().AddNode(destreamMinerRemote.Endpoint, true);

                    TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(destreamMinerLocal, destreamMinerRemote));

                    // Now disconnect the peers and mine block C2p on remote.
                    destreamMinerLocal.CreateRPCClient().RemoveNode(destreamMinerRemote.Endpoint);
                    TestHelper.WaitLoop(() => !TestHelper.IsNodeConnected(destreamMinerRemote));

                    // Mine block C2p.
                    destreamMinerRemote.GenerateDeStreamWithMiner(1);
                    Thread.Sleep(2000);

                    // Now reconnect nodes and mine block C1s before C2p arrives.
                    destreamMinerLocal.CreateRPCClient().AddNode(destreamMinerRemote.Endpoint, true);
                    destreamMinerLocal.GenerateDeStreamWithMiner(1);

                    // Mine block Dp.
                    uint256 dpHash = destreamMinerRemote.GenerateDeStreamWithMiner(1)[0];

                    // Now we wait until the local node's chain tip has correct hash of Dp.
                    TestHelper.WaitLoop(() => destreamMinerLocal.FullNode.Chain.Tip.HashBlock.Equals(dpHash));

                    // Then give it time to receive the block from the puller.
                    Thread.Sleep(2500);

                    // Check that local node accepted the Dp as consensus tip.
                    Assert.Equal(destreamMinerLocal.FullNode.ChainBehaviorState.ConsensusTip.HashBlock, dpHash);
                }
            }
            finally
            {
                Transaction.TimeStamp = false;
                Block.BlockSignature = false;
            }
        }

        /// <summary>
        /// This test simulates scenario from issue #862.
        /// <para>
        /// Connection scheme:
        /// Network - Node1 - MiningNode
        /// </para>
        /// </summary>
        [Fact]
        public void MiningNodeWithOneConnectionAlwaysSynced()
        {
            NetworkSimulator simulator = new NetworkSimulator();

            simulator.Initialize(4);

            var miner = simulator.Nodes[0];
            var connector = simulator.Nodes[1];
            var networkNode1 = simulator.Nodes[2];
            var networkNode2 = simulator.Nodes[3];

            // Connect nodes with each other. Miner is connected to connector and connector, node1, node2 are connected with each other.
            miner.CreateRPCClient().AddNode(connector.Endpoint, true);
            connector.CreateRPCClient().AddNode(networkNode1.Endpoint, true);
            connector.CreateRPCClient().AddNode(networkNode2.Endpoint, true);
            networkNode1.CreateRPCClient().AddNode(networkNode2.Endpoint, true);

            simulator.MakeSureEachNodeCanMineAndSync();

            int networkHeight = miner.FullNode.Chain.Height;
            Assert.Equal(networkHeight, simulator.Nodes.Count);

            // Random node on network generates a block.
            networkNode1.GenerateDeStream(1);

            // Wait until connector get the hash of network's block.
            while ((connector.FullNode.ChainBehaviorState.ConsensusTip.HashBlock != networkNode1.FullNode.ChainBehaviorState.ConsensusTip.HashBlock) ||
                   (networkNode1.FullNode.ChainBehaviorState.ConsensusTip.Height == networkHeight))
                Thread.Sleep(1);

            // Make sure that miner did not advance yet but connector did.
            Assert.NotEqual(miner.FullNode.Chain.Tip.HashBlock, networkNode1.FullNode.Chain.Tip.HashBlock);
            Assert.Equal(connector.FullNode.Chain.Tip.HashBlock, networkNode1.FullNode.Chain.Tip.HashBlock);
            Assert.Equal(miner.FullNode.Chain.Tip.Height, networkHeight);
            Assert.Equal(connector.FullNode.Chain.Tip.Height, networkHeight+1);

            // Miner mines the block.
            miner.GenerateDeStream(1);
            TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(miner));

            networkHeight++;

            // Make sure that at this moment miner's tip != network's and connector's tip.
            Assert.NotEqual(miner.FullNode.Chain.Tip.HashBlock, networkNode1.FullNode.Chain.Tip.HashBlock);
            Assert.Equal(connector.FullNode.Chain.Tip.HashBlock, networkNode1.FullNode.Chain.Tip.HashBlock);
            Assert.Equal(miner.FullNode.Chain.Tip.Height, networkHeight);
            Assert.Equal(connector.FullNode.Chain.Tip.Height, networkHeight);

            connector.GenerateDeStream(1);
            networkHeight++;

            int delay = 0;

            while (true)
            {
                Thread.Sleep(50);
                if (simulator.DidAllNodesReachHeight(networkHeight))
                    break;
                delay += 50;

                Assert.True(delay < 10 * 1000, "Miner node was not able to advance!");
            }

            Assert.Equal(networkNode1.FullNode.Chain.Tip.HashBlock, miner.FullNode.Chain.Tip.HashBlock);
        }
    }
}

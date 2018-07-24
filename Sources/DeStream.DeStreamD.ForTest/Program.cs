using System;
using System.Linq;
using System.Threading.Tasks;
using DeStream.Stratis.Bitcoin.Configuration;
using NBitcoin;
using NBitcoin.Protocol;
using Stratis.Bitcoin;
using Stratis.Bitcoin.Builder;
using Stratis.Bitcoin.Features.Api;
using Stratis.Bitcoin.Features.BlockStore;
using Stratis.Bitcoin.Features.Consensus;
using Stratis.Bitcoin.Features.MemoryPool;
using Stratis.Bitcoin.Features.Miner;
using Stratis.Bitcoin.Features.RPC;
using Stratis.Bitcoin.Features.Wallet;
using Stratis.Bitcoin.Features.Wallet.Interfaces;
using Stratis.Bitcoin.IntegrationTests.Common;
using Stratis.Bitcoin.IntegrationTests.Common.EnvironmentMockUpHelpers;
using Stratis.Bitcoin.Utilities;

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
                var node = new FullNodeBuilder()
                    .UseNodeSettings(nodeSettings)
                    .UseBlockStore()
                    .UsePosConsensus()
                    .UseMempool()
                    .UseWallet()
                    .AddPowPosMining()
                    .UseApi()
                    .AddRPC()
                    .Build();


                NodeBuilder builder = NodeBuilder.Create(node);
                CoreNode stratisSender = builder.CreateStratisPowNode();
                CoreNode stratisReceiver = builder.CreateStratisPowNode();
                builder.StartAll();
                stratisSender.NotInIBD();
                stratisReceiver.NotInIBD();

                // get a key from the wallet
                Mnemonic mnemonic1 = stratisSender.FullNode.WalletManager().CreateWallet("123456", "mywallet");
                Mnemonic mnemonic2 = stratisReceiver.FullNode.WalletManager().CreateWallet("123456", "mywallet");
                HdAddress addr = stratisSender.FullNode.WalletManager().GetUnusedAddress(new WalletAccountReference("mywallet", "account 0"));
                Wallet wallet = stratisSender.FullNode.WalletManager().GetWalletByName("mywallet");
                Key key = wallet.GetExtendedPrivateKeyForAddress("123456", addr).PrivateKey;

                stratisSender.SetDummyMinerSecret(new BitcoinSecret(key, stratisSender.FullNode.Network));
                int maturity = (int)stratisSender.FullNode.Network.Consensus.CoinbaseMaturity;
                stratisSender.GenerateStratis(maturity + 5);
                // wait for block repo for block sync to work

                TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(stratisSender));

                // the mining should add coins to the wallet
                long total = stratisSender.FullNode.WalletManager().GetSpendableTransactionsInWallet("mywallet").Sum(s => s.Transaction.Amount);


                var walletManager = ((FullNode)node).NodeService<IWalletManager>() as WalletManager;
                //HdAddress addr = ((FullNode)node).WalletManager().GetUnusedAddress(new WalletAccountReference("mywallet", "account 0"));
                walletManager.CreateWallet("123456", "mywallet");
                HdAddress sendto = walletManager.GetUnusedAddress(new WalletAccountReference("mywallet", "account 0"));
                var walletTransactionHandler = ((FullNode)node).NodeService<IWalletTransactionHandler>() as WalletTransactionHandler;

                var transactionBuildContext = CreateContext(
                    new WalletAccountReference("mywallet", "account 0"), "123456", sendto.ScriptPubKey, Money.COIN * 100, FeeType.Medium, 101);

                Transaction trx = walletTransactionHandler.BuildTransaction(transactionBuildContext);

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

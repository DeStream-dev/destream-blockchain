using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using DeStream.Stratis.Bitcoin.Configuration;
using Moq;
using NBitcoin;
using Stratis.Bitcoin;
using Stratis.Bitcoin.Features.Wallet;
using Stratis.Bitcoin.Features.Wallet.Interfaces;
using Stratis.Bitcoin.IntegrationTests.Common.EnvironmentMockUpHelpers;
using Stratis.Bitcoin.Utilities;
using static Stratis.Bitcoin.BlockPulling.BlockPuller;

namespace DeStream.DeStreamD.ForTest
{
    public class TestClassHelper
    {
        public static (ExtKey ExtKey, string ExtPubKey) GenerateAccountKeys(Wallet wallet, string password, string keyPath)
        {
            var accountExtKey = new ExtKey(Key.Parse(wallet.EncryptedSeed, password, wallet.Network), wallet.ChainCode);
            string accountExtendedPubKey = accountExtKey.Derive(new KeyPath(keyPath)).Neuter().ToString(wallet.Network);
            return (accountExtKey, accountExtendedPubKey);
        }

        public static (PubKey PubKey, BitcoinPubKeyAddress Address) GenerateAddressKeys(Wallet wallet, string accountExtendedPubKey, string keyPath)
        {
            PubKey addressPubKey = ExtPubKey.Parse(accountExtendedPubKey).Derive(new KeyPath(keyPath)).PubKey;
            BitcoinPubKeyAddress address = addressPubKey.GetAddress(wallet.Network);

            return (addressPubKey, address);
        }


        public static (ConcurrentChain chain, uint256 blockhash, Block block) CreateChainAndCreateFirstBlockWithPaymentToAddress(WalletManager walletManager, Network network, HdAddress address)
        {
            var chain = new ConcurrentChain(network);

            //var chain = walletManager.Chain;

            var block = new Block();
            //var block = network.GetGenesis().Header;

            block.Header.HashPrevBlock = chain.Tip.HashBlock;
            block.Header.Bits = block.Header.GetWorkRequired(network, chain.Tip);
            block.Header.UpdateTime(DateTimeOffset.UtcNow, network, chain.Tip);

            var coinbase = new Transaction();
            coinbase.AddInput(TxIn.CreateCoinbase(chain.Height + 1));
            coinbase.AddOutput(new TxOut(Get9Billion(), address.ScriptPubKey));

            block.AddTransaction(coinbase);
            block.Header.Nonce = 0;
            block.UpdateMerkleRoot();
            block.Header.PrecomputeHash();

            chain.SetTip(block.Header);

            return (chain, block.GetHash(), block);
        }

        public static TransactionData CreateTransactionDataFromFirstBlock((ConcurrentChain chain, uint256 blockHash, Block block) chainInfo)
        {
            Transaction transaction = chainInfo.block.Transactions[0];

            var addressTransaction = new TransactionData
            {
                Amount = transaction.TotalOut,
                BlockHash = chainInfo.blockHash,
                BlockHeight = chainInfo.chain.GetBlock(chainInfo.blockHash).Height,
                CreationTime = DateTimeOffset.FromUnixTimeSeconds(chainInfo.block.Header.Time),
                Id = transaction.GetHash(),
                Index = 0,
                ScriptPubKey = transaction.Outputs[0].ScriptPubKey,
            };
            return addressTransaction;
        }

        public static Transaction SetupValidTransaction(Wallet wallet, string password, HdAddress spendingAddress, PubKey destinationPubKey, HdAddress changeAddress, Money amount, Money fee)
        {
            TransactionData spendingTransaction = spendingAddress.Transactions.ElementAt(0);
            var coin = new Coin(spendingTransaction.Id, (uint)spendingTransaction.Index, spendingTransaction.Amount, spendingTransaction.ScriptPubKey);

            Key privateKey = Key.Parse(wallet.EncryptedSeed, password, wallet.Network);

            var builder = new TransactionBuilder(wallet.Network);
            Transaction tx = builder
                .AddCoins(new List<Coin> { coin })
                .AddKeys(new ExtKey(privateKey, wallet.ChainCode).Derive(new KeyPath(spendingAddress.HdPath)).GetWif(wallet.Network))
                .Send(destinationPubKey.ScriptPubKey, amount)
                .SetChange(changeAddress.ScriptPubKey)
                .SendFees(fee)
                .BuildTransaction(true);

            if (!builder.Verify(tx))
            {
                throw new WalletException("Could not build transaction, please make sure you entered the correct data.");
            }

            return tx;
        }

        public static Block AppendTransactionInNewBlockToChain(ConcurrentChain chain, Transaction transaction)
        {
            ChainedHeader last = null;
            uint nonce = RandomUtils.GetUInt32();
            Block block = chain.Network.Consensus.ConsensusFactory.CreateBlock();
            block.AddTransaction(transaction);
            block.UpdateMerkleRoot();
            block.Header.HashPrevBlock = chain.Tip.HashBlock;
            block.Header.Nonce = nonce;
            if (!chain.TrySetTip(block.Header, out last))
                throw new InvalidOperationException("Previous not existing");

            return block;
        }

        public static (Wallet wallet, ExtKey key) GenerateBlankWalletWithExtKey(string name, string password)
        {
            var mnemonic = new Mnemonic("grass industry beef stereo soap employ million leader frequent salmon crumble banana");
            ExtKey extendedKey = mnemonic.DeriveExtKey(password);

            var walletFile = new Wallet
            {
                Name = name,
                EncryptedSeed = extendedKey.PrivateKey.GetEncryptedBitcoinSecret(password, Network.DeStreamTest).ToWif(),
                ChainCode = extendedKey.ChainCode,
                CreationTime = DateTimeOffset.Now,
                Network = Network.DeStreamTest,
                AccountsRoot = new List<AccountRoot> { new AccountRoot() { Accounts = new List<HdAccount>(), CoinType = (CoinType)Network.DeStreamTest.Consensus.CoinType } },
            };

            return (walletFile, extendedKey);
        }

        //public static Wallet CreateFirstTransaction(DeStreamNodeSettings nodeSettings, ref DeStreamWalletManager walletManager, WalletSettings walletSettings,
        //    IWalletFeePolicy _walletFeePolicy)
        public static (Wallet wallet, Block block, ChainedHeader chainedHeader) CreateFirstTransaction(DeStreamNodeSettings nodeSettings, WalletManager walletManager, WalletSettings walletSettings,
            IWalletFeePolicy _walletFeePolicy)
        {
            Wallet wallet = GenerateBlankWalletWithExtKey("myWallet1", "password").wallet;
            (ExtKey ExtKey, string ExtPubKey) accountKeys = GenerateAccountKeys(wallet, "password", "m/44'/0'/0'");
            (PubKey PubKey, BitcoinPubKeyAddress Address) spendingKeys = GenerateAddressKeys(wallet, accountKeys.ExtPubKey, "0/0");
            (PubKey PubKey, BitcoinPubKeyAddress Address) destinationKeys = GenerateAddressKeys(wallet, accountKeys.ExtPubKey, "0/1");
            (PubKey PubKey, BitcoinPubKeyAddress Address) changeKeys = GenerateAddressKeys(wallet, accountKeys.ExtPubKey, "1/0");

            var spendingAddress = new HdAddress
            {
                Index = 0,
                HdPath = $"m/44'/0'/0'/0/0",
                Address = spendingKeys.Address.ToString(),
                Pubkey = spendingKeys.PubKey.ScriptPubKey,
                ScriptPubKey = spendingKeys.Address.ScriptPubKey,
                Transactions = new List<TransactionData>()
            };

            var destinationAddress = new HdAddress
            {
                Index = 1,
                HdPath = $"m/44'/0'/0'/0/1",
                Address = destinationKeys.Address.ToString(),
                Pubkey = destinationKeys.PubKey.ScriptPubKey,
                ScriptPubKey = destinationKeys.Address.ScriptPubKey,
                Transactions = new List<TransactionData>()
            };

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
            (ConcurrentChain chain, uint256 blockhash, Block block) chainInfo = CreateChainAndCreateFirstBlockWithPaymentToAddress(walletManager, wallet.Network, spendingAddress);

            TransactionData spendingTransaction = CreateTransactionDataFromFirstBlock(chainInfo);
            spendingAddress.Transactions.Add(spendingTransaction);

            // setup a payment to yourself in a new block.
            Transaction transaction = SetupValidTransaction(wallet, "password", spendingAddress, destinationKeys.PubKey, changeAddress, new Money(7500), new Money(5000));
            Block block = AppendTransactionInNewBlockToChain(chainInfo.chain, transaction);

            wallet.AccountsRoot.ElementAt(0).Accounts.Add(new HdAccount
            {
                Index = 1,
                Name = "account1",
                HdPath = "m/44'/0'/0'",
                ExtendedPubKey = accountKeys.ExtPubKey,
                //ExternalAddresses = new List<HdAddress> { spendingAddress, destinationAddress },
                ExternalAddresses = new List<HdAddress> { spendingAddress },
                //InternalAddresses = new List<HdAddress> { changeAddress }
                InternalAddresses = new List<HdAddress> { destinationAddress }
            });

            var walletFeePolicy = new Mock<IWalletFeePolicy>();
            walletFeePolicy.Setup(w => w.GetMinimumFee(258, 50))
                .Returns(new Money(5000));
            HdAddress spentAddressResult0 = wallet.AccountsRoot.ElementAt(0).Accounts.ElementAt(0).ExternalAddresses.ElementAt(0);

            //var _walletManager = new WalletManager(nodeSettings.LoggerFactory, Network.Main, chainInfo.chain, nodeSettings, new Mock<WalletSettings>().Object,
            //    nodeSettings.DataFolder, walletFeePolicy.Object, new Mock<IAsyncLoopFactory>().Object, new NodeLifetime(), DateTimeProvider.Default);

            //walletManager = new DeStreamWalletManager(nodeSettings.LoggerFactory, nodeSettings.Network, chainInfo.chain, nodeSettings,
            //    walletSettings,
            //    nodeSettings.DataFolder, walletFeePolicy.Object, walletManager.AsyncLoopFactory, walletManager.NodeLifetime, walletManager.DateTimeProvider);

            //walletManager.Wallets.Add(wallet);
            //walletManager.LoadKeysLookupLock();
            //walletManager.WalletTipHash = block.Header.GetHash();

            ChainedHeader chainedBlock = chainInfo.chain.GetBlock(block.GetHash());
            ////walletManager.ProcessTransaction(transaction, null, block, false);
            //walletManager.ProcessBlock(block, chainedBlock);
            return (wallet, block, chainedBlock);

            //HdAddress spentAddressResult = wallet.AccountsRoot.ElementAt(0).Accounts.ElementAt(0).ExternalAddresses.ElementAt(0);
            //return wallet;
        }

        public static Money Get9Billion()
        {
            return new Money(9000000000);
        }

        public static Block CreateTestBlock(FullNode fullNode, Key key)
        {
            //FullNode fullNode = (this.runner as StratisBitcoinPowRunner).FullNode;

            BitcoinSecret dest = new BitcoinSecret(key, fullNode.Network);
            var blocks = new List<Block>();


            List<Transaction> passedTransactions = null;
            uint nonce = 0;
            var block = new Block();
            block.Header.HashPrevBlock = fullNode.Chain.Tip.HashBlock;
            block.Header.Bits = block.Header.GetWorkRequired(fullNode.Network, fullNode.Chain.Tip);
            block.Header.UpdateTime(DateTimeOffset.UtcNow, fullNode.Network, fullNode.Chain.Tip);
            var coinbase = new Transaction();
            coinbase.AddInput(TxIn.CreateCoinbase(fullNode.Chain.Height + 1));
            coinbase.AddOutput(new TxOut(Get9Billion(), dest.GetAddress()));
            block.AddTransaction(coinbase);
            if (passedTransactions?.Any() ?? false)
            {
                passedTransactions = Reorder(passedTransactions);
                block.Transactions.AddRange(passedTransactions);
            }
            block.UpdateMerkleRoot();
            while (!block.CheckProofOfWork())
                block.Header.Nonce = ++nonce;
            blocks.Add(block);
            uint256 blockHash = block.GetHash();
            var newChain = new ChainedHeader(block.Header, blockHash, fullNode.Chain.Tip);
            ChainedHeader oldTip = fullNode.Chain.SetTip(newChain);
            fullNode.ConsensusLoop().Puller.InjectBlock(blockHash, new DownloadedBlock { Length = block.GetSerializedSize(), Block = block }, CancellationToken.None);
            return block;
        }

        private class TransactionNode
        {
            public uint256 Hash = null;
            public Transaction Transaction = null;
            public List<TransactionNode> DependsOn = new List<TransactionNode>();

            public TransactionNode(Transaction tx)
            {
                this.Transaction = tx;
                this.Hash = tx.GetHash();
            }
        }

        private static List<Transaction> Reorder(List<Transaction> transactions)
        {
            if (transactions.Count == 0)
                return transactions;

            var result = new List<Transaction>();
            Dictionary<uint256, TransactionNode> dictionary = transactions.ToDictionary(t => t.GetHash(), t => new TransactionNode(t));
            foreach (TransactionNode transaction in dictionary.Select(d => d.Value))
            {
                foreach (TxIn input in transaction.Transaction.Inputs)
                {
                    TransactionNode node = dictionary.TryGet(input.PrevOut.Hash);
                    if (node != null)
                    {
                        transaction.DependsOn.Add(node);
                    }
                }
            }

            while (dictionary.Count != 0)
            {
                foreach (TransactionNode node in dictionary.Select(d => d.Value).ToList())
                {
                    foreach (TransactionNode parent in node.DependsOn.ToList())
                    {
                        if (!dictionary.ContainsKey(parent.Hash))
                            node.DependsOn.Remove(parent);
                    }

                    if (node.DependsOn.Count == 0)
                    {
                        result.Add(node.Transaction);
                        dictionary.Remove(node.Hash);
                    }
                }
            }

            return result;
        }

    }
}
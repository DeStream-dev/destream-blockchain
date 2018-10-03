using System;
using System.Collections.Generic;
using System.Linq;
using NBitcoin.BouncyCastle.Math;
using NBitcoin.DataEncoders;

namespace NBitcoin.Networks
{
    public class DeStreamMain : Network
    {
        public DeStreamMain()
        {
            var initialWalletAddresses = new[]
            {
                "TWyLf11aUSQvorSvG4oc3asMGXbqkf8MEa",
                "TSX8RGmEod8K4a2SvPPWZtmJ5KtrBzzXSw",
                "TTp1D1NrV1uwbuL2YvWm46M3xY8nYQLRHr",
                "TBgvA3dKhGMGeWXpzCG9UUviXLFjZjsQ2S",
                "TV37E8whdDUEzVFSsWRHHcj7bWbeDTv9gw",
                "TWyiGrPmuKvcMj9s9SGR4BWzMxhZQXJxZk",
                "TNL98Epf3ASKFod2QuincwNi2CxHLkkjMD",
                "TG3N5ARtJaajqdNHgC9pxnW5kL9CeWkcDa",
                "TA9GwihBb9KcW3evjxdVkUh1XdQ5wbEcif",
                "TBxudKvSsw1hL7aGf9a34dSdxV4e97dx5y"
            };
            const decimal initialCoins = 6000000000;
            const int numberOfEmissionTransactions = 6;

            var messageStart = new byte[4];
            messageStart[0] = 0x70;
            messageStart[1] = 0x35;
            messageStart[2] = 0x22;
            messageStart[3] = 0x05;
            uint magic = BitConverter.ToUInt32(messageStart, 0);

            this.Name = "DeStreamMain";
            this.RootFolderName = DeStreamRootFolderName;
            this.DefaultConfigFilename = DeStreamDefaultConfigFilename;
            this.Magic = magic;
            this.DefaultPort = 0xDE01; // 56833,
            this.RPCPort = 0xDE00; // 56832,
            this.MinTxFee = 10000;
            this.FallbackFee = 60000;
            this.MinRelayTxFee = 10000;
            this.MaxTimeOffsetSeconds = StratisMaxTimeOffsetSeconds;
            this.MaxTipAge = StratisDefaultMaxTipAgeInSeconds;
            this.CoinTicker = "DST";

            this.Consensus.SubsidyHalvingInterval = 210000;
            this.Consensus.MajorityEnforceBlockUpgrade = 750;
            this.Consensus.MajorityRejectBlockOutdated = 950;
            this.Consensus.MajorityWindow = 1000;
            this.Consensus.BuriedDeployments[BuriedDeployments.BIP34] = 0;
            this.Consensus.BuriedDeployments[BuriedDeployments.BIP65] = 0;
            this.Consensus.BuriedDeployments[BuriedDeployments.BIP66] = 0;
            this.Consensus.BIP34Hash =
                new uint256("0x000000000000024b89b42a942fe0d9fea3bb44ab7bd1b19115dd6a759c0808b8");
            this.Consensus.PowLimit =
                new Target(new uint256("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff"));
            this.Consensus.PowTargetTimespan = TimeSpan.FromSeconds(14 * 24 * 60 * 60); // two weeks
            this.Consensus.PowTargetSpacing = TimeSpan.FromSeconds(10 * 60);
            this.Consensus.PowAllowMinDifficultyBlocks = false;
            this.Consensus.PowNoRetargeting = false;
            this.Consensus.RuleChangeActivationThreshold = 1916; // 95% of 2016
            this.Consensus.MinerConfirmationWindow = 2016; // nPowTargetTimespan / nPowTargetSpacing
            this.Consensus.LastPOWBlock = 12500;
            this.Consensus.IsProofOfStake = true;
            this.Consensus.ConsensusFactory = new PosConsensusFactory {Consensus = this.Consensus};
            this.Consensus.ProofOfStakeLimit = new BigInteger(uint256
                .Parse("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff").ToBytes(false));
            this.Consensus.ProofOfStakeLimitV2 = new BigInteger(uint256
                .Parse("000000000000ffffffffffffffffffffffffffffffffffffffffffffffffffff").ToBytes(false));
            this.Consensus.CoinType = 105;
            this.Consensus.DefaultAssumeValid =
                new uint256("0x55a8205ae4bbf18f4d238c43f43005bd66e0b1f679b39e2c5c62cf6903693a5e"); // 795970
            this.Consensus.MaxMoney = long.MaxValue;

            this.Checkpoints = new Dictionary<int, CheckpointInfo>();
            // TODO: Add genesis and premine block to Checkpoints
            // First parameter - block height
            // { 0, new CheckpointInfo(new uint256("0x00000e246d7b73b88c9ab55f2e5e94d9e22d471def3df5ea448f5576b1d156b9"), new uint256("0x0000000000000000000000000000000000000000000000000000000000000000")) },

            this.Base58Prefixes = new byte[12][];
            this.Base58Prefixes[(int) Base58Type.PUBKEY_ADDRESS] = new byte[] {63};
            this.Base58Prefixes[(int) Base58Type.SCRIPT_ADDRESS] = new byte[] {125};
            this.Base58Prefixes[(int) Base58Type.SECRET_KEY] = new byte[] {63 + 128};
            this.Base58Prefixes[(int) Base58Type.ENCRYPTED_SECRET_KEY_NO_EC] = new byte[] {0x01, 0x42};
            this.Base58Prefixes[(int) Base58Type.ENCRYPTED_SECRET_KEY_EC] = new byte[] {0x01, 0x43};
            this.Base58Prefixes[(int) Base58Type.EXT_PUBLIC_KEY] = new byte[] {0x04, 0x88, 0xB2, 0x1E};
            this.Base58Prefixes[(int) Base58Type.EXT_SECRET_KEY] = new byte[] {0x04, 0x88, 0xAD, 0xE4};
            this.Base58Prefixes[(int) Base58Type.PASSPHRASE_CODE] =
                new byte[] {0x2C, 0xE9, 0xB3, 0xE1, 0xFF, 0x39, 0xE2};
            this.Base58Prefixes[(int) Base58Type.CONFIRMATION_CODE] = new byte[] {0x64, 0x3B, 0xF6, 0xA8, 0x9A};
            this.Base58Prefixes[(int) Base58Type.STEALTH_ADDRESS] = new byte[] {0x2a};
            this.Base58Prefixes[(int) Base58Type.ASSET_ID] = new byte[] {23};
            this.Base58Prefixes[(int) Base58Type.COLORED_ADDRESS] = new byte[] {0x13};

            var encoder = new Bech32Encoder("bc");
            this.Bech32Encoders = new Bech32Encoder[2];
            this.Bech32Encoders[(int) Bech32Type.WITNESS_PUBKEY_ADDRESS] = encoder;
            this.Bech32Encoders[(int) Bech32Type.WITNESS_SCRIPT_ADDRESS] = encoder;

            this.DNSSeeds = new List<DNSSeedData>
            {
                new DNSSeedData("node1.destream.io", "node1.destream.io"),
                new DNSSeedData("node2.destream.io", "node2.destream.io")
            };

            string[] seedNodes = {"95.128.181.103", "95.128.181.80"};
            this.SeedNodes = this.ConvertToNetworkAddresses(seedNodes, this.DefaultPort).ToList();

            // Create the genesis block.
            this.GenesisTime = 1470467000;
            this.GenesisNonce = 1831645;
            this.GenesisBits = 0x1e0fffff;
            this.GenesisVersion = 1;
            this.GenesisReward = Money.Coins(initialCoins);
            this.GenesisWalletAddress = initialWalletAddresses.First();

            this.Genesis = this.CreateDeStreamGenesisBlock(this.Consensus.ConsensusFactory, this.GenesisTime,
                this.GenesisNonce, this.GenesisBits, this.GenesisVersion, this.GenesisReward,
                initialWalletAddresses);
            this.Consensus.HashGenesisBlock = this.Genesis.GetHash();

//            Assert(this.Consensus.HashGenesisBlock == uint256.Parse("c5974b227ccb19ebd97578285a5937bb4bfb6dcdbf473966d8a2f9c714a8dbb0"));
//            Assert(this.Genesis.Header.HashMerkleRoot == uint256.Parse("9e3fff58fb1ba15a69198e22d99572fa024afb754bfe1d3b8d28b86fd9de62df"));
        }

        protected Block CreateDeStreamGenesisBlock(ConsensusFactory consensusFactory, uint nTime, uint nNonce,
            uint nBits, int nVersion, Money initialCoins, string[] initialWalletAddresses)
        {
            const string pszTimestamp = "DESTREAM IS THE FIRST DECENTRALIZED GLOBAL FINANCIAL ECOSYSTEM FOR STREAMERS";

            Transaction txNew = consensusFactory.CreateTransaction();
            txNew.Version = 1;
            txNew.Time = nTime;
            txNew.AddInput(new TxIn
            {
                ScriptSig = new Script(Op.GetPushOp(0), new Op
                {
                    Code = (OpcodeType) 0x1,
                    PushData = new[] {(byte) 42}
                }, Op.GetPushOp(Encoders.ASCII.DecodeData(pszTimestamp)))
            });

            byte[] prefix = this.Base58Prefixes[(int) Base58Type.PUBKEY_ADDRESS];
            foreach (string initialWalletAddress in initialWalletAddresses)
            {
                byte[] destinationPublicKey =
                    Encoders.Base58Check.DecodeData(initialWalletAddress).Skip(prefix.Length).ToArray();
                Script destination = new KeyId(new uint160(destinationPublicKey)).ScriptPubKey;
                txNew.AddOutput(new TxOut(initialCoins / initialWalletAddresses.Length, destination));
            }

            Block genesis = consensusFactory.CreateBlock();
            genesis.Header.BlockTime = Utils.UnixTimeToDateTime(nTime);
            genesis.Header.Bits = nBits;
            genesis.Header.Nonce = nNonce;
            genesis.Header.Version = nVersion;
            genesis.Transactions.Add(txNew);
            genesis.Header.HashPrevBlock = uint256.Zero;
            genesis.UpdateMerkleRoot();
            return genesis;
        }
    }
}
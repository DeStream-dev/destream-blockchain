using System;
using System.Collections.Generic;
using System.Linq;
using NBitcoin;
using NBitcoin.BouncyCastle.Math;
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;

namespace Stratis.Bitcoin.Networks
{
    public class DeStreamTest : DeStreamNetwork
    {
        public DeStreamTest()
        {// The message start string is designed to be unlikely to occur in normal data.
            // The characters are rarely used upper ASCII, not valid as UTF-8, and produce
            // a large 4-byte int at any alignment.
            var messageStart = new byte[4];
            messageStart[0] = 0xFD;
            messageStart[1] = 0xFC;
            messageStart[2] = 0xFC;
            messageStart[3] = 0xFD;
            uint magic = BitConverter.ToUInt32(messageStart, 0);

            this.Name = "DeStreamTest";
            
            this.Magic = magic;
            this.DefaultPort = 56849;
            this.DefaultMaxOutboundConnections = 16;
            this.DefaultMaxInboundConnections = 109;
            this.RPCPort = 56848;
            this.MaxTipAge = DeStreamDefaultMaxTipAgeInSeconds;
            this.MinTxFee = 10000;
            this.FallbackFee = 60000;
            this.MinRelayTxFee = 10000;
            this.RootFolderName = DeStreamRootFolderName;
            this.DefaultConfigFilename = DeStreamDefaultConfigFilename;
            this.MaxTimeOffsetSeconds = DeStreamMaxTimeOffsetSeconds;
            this.CoinTicker = "TDST";
            this.DeStreamFeePart = 0.9;
            this.FeeRate = 0.0077;
            
            var consensusFactory = new PosConsensusFactory();
            
            // Create the genesis block.
            var initialWalletAddresses = new[]
            {
                "TrpCjf3qT6QXckyXqF8nT1rCQzD2g8S6ES",
                "Tqe8mF9go4eie1gukcchHRFKFEfd3X75dc",
                "TqgsrrXENYgiginKbxvfTbeWRPYtR4NdMM",
                "TiG1yaj7qaFfX8C4aj4T4DQK5KUxmbLGra",
                "ThvAAkHQNbHUegDe7Y4AVFJuDeMZHSkU2m",
                "TboaVBt66aZcFE7EhBwWrm5jCL9Xu832Ho",
                "Tw5FZ1FrCCNkHD68GEbt7LvovoAj2zfmia",
                "TmwSHokx4v2yWiUBpen5zx9Laxf5Kvzcn7",
                "ThxztyUpm2ak44pBTcQvbRc9ERHgfP72DV",
                "Tmewpbmy6SDsyZHszVfizEsR66grcJAf3J",
                "Tg4P3HfrTprrUi2fyxzp5qYcXB2nYXgdmV",
                "TetkuZ1vQVteMd4BARVcN2x9YjbXyUirPW",
                "TvAkCawusnDEyNJLMRohEaKeyoGGK6Lz4B",
                "TpfBFwygXdzq3DWrzUXDvHRcin4RsHQbhV",
                "TuqX4d9tRHzq7Yf9uuEXLZEUkBuKuLsc5L",
                "ThzLVN9n9U3AGZVgWU1b561WBR9TEN6XbD",
                "TcktjDxbrNqdgW84xwesWG7hJphnVvUDSb",
                "Tnp7gDYkQ3MtFVXQ3ZgkrZGmRErdqcdRQM",
                "TvZ1syCg7ffJLX7Y2GCAZtYGXbJDrvNZy3",
                "TgnFxUtqFsMXRm5kksd6o59njSCu67QeNi"
            };
            const decimal initialCoins = 6000000000;
            
            this.GenesisTime = 1470467000;
            this.GenesisNonce = 1831645;
            this.GenesisBits = 0x1e0fffff;
            this.GenesisVersion = 1;
            this.GenesisReward = Money.Coins(initialCoins);
            this.GenesisWalletAddress = initialWalletAddresses.First();

            var genesisBlock = this.CreateDeStreamGenesisBlock(this.Consensus.ConsensusFactory, this.GenesisTime,
                this.GenesisNonce, this.GenesisBits, this.GenesisVersion, this.GenesisReward,
                initialWalletAddresses);

            this.Genesis = genesisBlock;
            
            // Taken from StratisX.
            var consensusOptions = new PosConsensusOptions(
                maxBlockBaseSize: 1_000_000,
                maxStandardVersion: 2,
                maxStandardTxWeight: 100_000,
                maxBlockSigopsCost: 20_000,
                maxStandardTxSigopsCost: 20_000 / 5
            );
            
            var buriedDeployments = new BuriedDeploymentsArray
            {
                [BuriedDeployments.BIP34] = 0,
                [BuriedDeployments.BIP65] = 0,
                [BuriedDeployments.BIP66] = 0
            };
            
            //TODO: BIP9

            this.Consensus = new NBitcoin.Consensus(
                consensusFactory: consensusFactory,
                consensusOptions: consensusOptions,
                coinType: 1,
                hashGenesisBlock: genesisBlock.GetHash(),
                subsidyHalvingInterval: 210000,
                majorityEnforceBlockUpgrade: 750,
                majorityRejectBlockOutdated: 950,
                majorityWindow: 1000,
                buriedDeployments: buriedDeployments,
                bip9Deployments: null, //TODO: BIP9
                bip34Hash: new uint256("0x000000000000024b89b42a942fe0d9fea3bb44ab7bd1b19115dd6a759c0808b8"),
                ruleChangeActivationThreshold: 1916, // 95% of 2016
                minerConfirmationWindow: 2016, // nPowTargetTimespan / nPowTargetSpacing
                maxReorgLength: 500,
                defaultAssumeValid: new uint256("0x55a8205ae4bbf18f4d238c43f43005bd66e0b1f679b39e2c5c62cf6903693a5e"), // 795970
                maxMoney: long.MaxValue,
                coinbaseMaturity: 10,
                premineHeight: 0,
                premineReward: Money.Zero,
                proofOfWorkReward: Money.Zero,
                powTargetTimespan: TimeSpan.FromSeconds(14 * 24 * 60 * 60), // two weeks
                powTargetSpacing: TimeSpan.FromSeconds(10 * 60),
                powAllowMinDifficultyBlocks: false,
                powNoRetargeting: false,
                powLimit: new Target(new uint256("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
                minimumChainWork: null,
                isProofOfStake: true,
                lastPowBlock: 12500,
                proofOfStakeLimit: new BigInteger(uint256.Parse("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff").ToBytes(false)),
                proofOfStakeLimitV2: new BigInteger(uint256.Parse("000000000000ffffffffffffffffffffffffffffffffffffffffffffffffffff").ToBytes(false)),
                proofOfStakeReward: Money.Zero
            );
            
            this.Base58Prefixes = new byte[12][];
            this.Base58Prefixes[(int) Base58Type.PUBKEY_ADDRESS] = new byte[] {66};
            this.Base58Prefixes[(int) Base58Type.SCRIPT_ADDRESS] = new byte[] {128};
            this.Base58Prefixes[(int) Base58Type.SECRET_KEY] = new byte[] {66 + 128};
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

            this.Checkpoints = new Dictionary<int, CheckpointInfo>();
            // TODO: Add genesis and premine block to Checkpoints
            // First parameter - block height
            // { 0, new CheckpointInfo(new uint256("0x00000e246d7b73b88c9ab55f2e5e94d9e22d471def3df5ea448f5576b1d156b9"), new uint256("0x0000000000000000000000000000000000000000000000000000000000000000")) },

            var encoder = new Bech32Encoder("bc");
            this.Bech32Encoders = new Bech32Encoder[2];
            this.Bech32Encoders[(int) Bech32Type.WITNESS_PUBKEY_ADDRESS] = encoder;
            this.Bech32Encoders[(int) Bech32Type.WITNESS_SCRIPT_ADDRESS] = encoder;
           
            this.DNSSeeds = new List<DNSSeedData>
            {
                new DNSSeedData("testnode1.destream.io", "testnode1.destream.io")
            };

            this.SeedNodes = new List<NetworkAddress>();

            this.DeStreamWallets = new LinkedList<string>(new[]
            {
                "TxLYmDJc9bdYLrcT8v3DhUQFD7vnd7hq5n",
                "Ti4QCbGneeaYstSzy1Ak5c9QaWo7sHGznc",
                "Tk4sxrgogk6SE2kPXzCugkEyc9K4dsQPos",
                "TuBB9EEL7EoyWENdk81NuNWKqhpzRK4c4f",
                "Tkn6StGnSoSmtMcVEc2AXf31L6fht6KbHR",
                "TshSmiSkGXzXHdDdMPHYbRpaSR9fKxW1zD",
                "Tm9DdZE7rjyjHpyLWn8rLviP5vcYUdFTuC",
                "TZWfMcNZsJE76LqVxJUu7x3GhxN5ZJ9Uab",
                "TcNV9kvXjHzzu321GZNsusnxds1xoduDhG",
                "TggdqV7oMwcG2ad3xj4TyFMuGYqA8skCAN",
                "TmxHkKHfoEWiJhWyMgoyTfMPFK3vPvAbXi",
                "Tp3kjf2YmcVjJfpMR2VveS7T3kzazHNnkg",
                "TixpciA2nv2t8msRdYDSkTibL34EjKSFXZ",
                "TgySERJHqzEiwqnessVVBLY9NY8PwqvWHK",
                "Th2qkyYrqY5ZrSEzsZ4K1KqYtHfjnXeAJY",
                "TfLnAv1UqF672dkfftAVhxfuZLdeVnZ6nZ",
                "TsWPtxeTpDSoAp1pQfDzpeJegQMiwtxfw6",
                "TffHpyMjrNNxez37QUzDuMmwa4MrZmxWWR",
                "ThRCqMSWZmp5VWVzob1pfjqje6SqosP8Uw",
                "TxMpDfwHb5SZ9fzNMmeLthTd1D4BZ1EGyQ"
            }.OrderBy(p => Guid.NewGuid()));
            
            Assert(this.Consensus.HashGenesisBlock ==
                   uint256.Parse("95dfb30e229e18197a812ece5d8d6c03efc9b9b65a9122a73f17d99613841b1b"));
            Assert(this.Genesis.Header.HashMerkleRoot ==
                   uint256.Parse("6598d7cc968eae6d6e66e7ac88707f5e0948b816dc8ba52433d7edc1a1f2c6a3"));
        }

        protected Block CreateDeStreamGenesisBlock(ConsensusFactory consensusFactory, uint nTime, uint nNonce,
            uint nBits, int nVersion, Money initialCoins, string[] initialWalletAddresses)
        {
            const string pszTimestamp =
                "DESTREAM IS THE FIRST DECENTRALIZED GLOBAL FINANCIAL ECOSYSTEM FOR STREAMERS " +
                "https://sputniknews.com/science/201810281069300462-ai-cyborg-sophia-gets-robot-visa/";

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
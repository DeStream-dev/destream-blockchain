using System;
using System.Collections.Generic;
using System.Linq;
using NBitcoin.Protocol;

namespace NBitcoin.Networks
{
    public class DeStreamTest : DeStreamMain
    {
        public DeStreamTest() //: base()
        {
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

            // The message start string is designed to be unlikely to occur in normal data.
            // The characters are rarely used upper ASCII, not valid as UTF-8, and produce
            // a large 4-byte int at any alignment.
            var messageStart = new byte[4];
            messageStart[0] = 0xFD;
            messageStart[1] = 0xFC;
            messageStart[2] = 0xFC;
            messageStart[3] = 0xFD;
            uint magic = BitConverter.ToUInt32(messageStart, 0); // 0x11213171;

            this.Name = "DeStreamTest";
            this.Magic = magic;
            this.DefaultPort = 0xDE11; // 56849,
            this.RPCPort = 0xDE10; // 56848,
            this.CoinTicker = "TDST";

            this.Consensus.PowLimit =
                new Target(new uint256("0000ffff00000000000000000000000000000000000000000000000000000000"));
            this.Consensus.PowTargetTimespan = TimeSpan.FromSeconds(14 * 24 * 60 * 60); // two weeks
            this.Consensus.PowTargetSpacing = TimeSpan.FromSeconds(10 * 60);
            this.Consensus.PowAllowMinDifficultyBlocks = false;
            this.Consensus.PowNoRetargeting = false;
            this.Consensus.RuleChangeActivationThreshold = 1916; // 95% of 2016
            this.Consensus.MinerConfirmationWindow = 2016; // nPowTargetTimespan / nPowTargetSpacing
            this.Consensus.LastPOWBlock = 12500;
            this.Consensus.DefaultAssumeValid =
                new uint256("0x98fa6ef0bca5b431f15fd79dc6f879dc45b83ed4b1bbe933a383ef438321958e"); // 372652
            this.Consensus.CoinbaseMaturity = 10;
            this.Consensus.MaxMoney = long.MaxValue;
            this.Consensus.ProofOfWorkReward = Money.Zero;
            this.Consensus.ProofOfStakeReward = Money.Zero;
            this.Consensus.LastPOWBlock = 12500;
            this.Consensus.CoinType = 1;

            this.DeStreamFeePart = 0.9;
            this.FeeRate = 0.0077;

            this.Base58Prefixes[(int) Base58Type.PUBKEY_ADDRESS] = new byte[] {66};
            this.Base58Prefixes[(int) Base58Type.SCRIPT_ADDRESS] = new byte[] {128};
            this.Base58Prefixes[(int) Base58Type.SECRET_KEY] = new byte[] {66 + 128};

            this.Checkpoints = new Dictionary<int, CheckpointInfo>();
            // TODO: Add genesis and premine block to Checkpoints
            // First parameter - block height
            // { 0, new CheckpointInfo(new uint256("0x00000e246d7b73b88c9ab55f2e5e94d9e22d471def3df5ea448f5576b1d156b9"), new uint256("0x0000000000000000000000000000000000000000000000000000000000000000")) },

            this.DNSSeeds = new List<DNSSeedData>
            {
                new DNSSeedData("testnode1.destream.io", "testnode1.destream.io")
            };

            this.SeedNodes = new List<NetworkAddress>();

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
            this.Genesis.Header.Time =
                (uint) new DateTimeOffset(2018, 09, 24, 16, 13, 00, TimeSpan.FromHours(3)).ToUnixTimeSeconds();
            this.Genesis.Header.Nonce = 2433759;
            this.Genesis.Header.Bits = this.Consensus.PowLimit;
            this.Consensus.HashGenesisBlock = this.Genesis.GetHash();

//            Assert(this.Consensus.HashGenesisBlock == uint256.Parse("c5974b227ccb19ebd97578285a5937bb4bfb6dcdbf473966d8a2f9c714a8dbb0"));
//            Assert(this.Genesis.Header.HashMerkleRoot == uint256.Parse("9e3fff58fb1ba15a69198e22d99572fa024afb754bfe1d3b8d28b86fd9de62df"));
        }
    }
}
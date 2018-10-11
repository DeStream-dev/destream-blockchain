using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using NBitcoin.Protocol;

namespace NBitcoin.Networks
{
    public class DeStreamTest : DeStreamMain
    {
        public DeStreamTest() //: base()
        {
            var initialWalletAddresses = new []{
                "DC6UcLUzq645UeqCkdk4iJk9tvMVDQ2Ytd",
                "D9CKCEtU5cJ5BReBwf4YnWpSqcC7tr1oXv",
                "DU3cTLWubkzMRGoCSef1G1Jp1tj8z9TGPD",
                "D95x2iYdVVUwY5RnPjBmDKiJHToTgHhdor",
                "DPPnSDe416McZ2CKgmUagnJwXZuZ8b31ZM",
                "DHdc7gkwZRpKPTZzEf8TBQEthmfxuAJoUM",
                "DJzLTGxadMGnHqByQtyUW3zsLq5f7mSvJz",
                "D7UwtqLsCNkKb94tb6TiagUHRgF4UDEXMt",
                "D7a8q2Ldfmh1vBaGrANPFwyKU7oNKBRtQH",
                "DDmLwBBEoerPy8nZCAxcoyzwGwBs9zUhFq"
            };
            const decimal initialCoins = 6000000000;

            // The message start string is designed to be unlikely to occur in normal data.
            // The characters are rarely used upper ASCII, not valid as UTF-8, and produce
            // a large 4-byte int at any alignment.
            var messageStart = new byte[4];
            messageStart[0] = 0x71;
            messageStart[1] = 0x31;
            messageStart[2] = 0x21;
            messageStart[3] = 0x11;
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
            this.Consensus.CoinbaseMaturity = 1;
            this.Consensus.MaxMoney = long.MaxValue;
            this.Consensus.ProofOfWorkReward = Money.Zero;
            this.Consensus.ProofOfStakeReward = Money.Zero;
            this.Consensus.LastPOWBlock = 12500;
            this.Consensus.CoinType = 3564;

            this.DeStreamFeePart = 0.9;
            this.FeeRate = 0.0077;

            this.Base58Prefixes[(int) Base58Type.PUBKEY_ADDRESS] = new byte[] {30};
            this.Base58Prefixes[(int) Base58Type.SCRIPT_ADDRESS] = new byte[] {90};
            this.Base58Prefixes[(int) Base58Type.SECRET_KEY] = new byte[] {30 + 90};

            this.Checkpoints = new Dictionary<int, CheckpointInfo>();
            // TODO: Add genesis and premine block to Checkpoints
            // First parameter - block height
            // { 0, new CheckpointInfo(new uint256("0x00000e246d7b73b88c9ab55f2e5e94d9e22d471def3df5ea448f5576b1d156b9"), new uint256("0x0000000000000000000000000000000000000000000000000000000000000000")) },

            this.DNSSeeds = new List<DNSSeedData>
            {
                new DNSSeedData("testnode1.destream.io", "testnode1.destream.io")
            };

            this.SeedNodes = new List<NetworkAddress>
            {
                new NetworkAddress(IPAddress.Parse("95.128.181.196"), this.DefaultPort), //peak-srv-12
                new NetworkAddress(IPAddress.Parse("40.121.9.206"), this.DefaultPort)
            };

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
            this.Genesis.Header.Time = (uint) new DateTimeOffset(2018,09,24,16,13,00, TimeSpan.FromHours(3)).ToUnixTimeSeconds();
            this.Genesis.Header.Nonce = 2433759;
            this.Genesis.Header.Bits = this.Consensus.PowLimit;
            this.Consensus.HashGenesisBlock = this.Genesis.GetHash();
            
//            Assert(this.Consensus.HashGenesisBlock == uint256.Parse("c5974b227ccb19ebd97578285a5937bb4bfb6dcdbf473966d8a2f9c714a8dbb0"));
//            Assert(this.Genesis.Header.HashMerkleRoot == uint256.Parse("9e3fff58fb1ba15a69198e22d99572fa024afb754bfe1d3b8d28b86fd9de62df"));
        }
    }
}
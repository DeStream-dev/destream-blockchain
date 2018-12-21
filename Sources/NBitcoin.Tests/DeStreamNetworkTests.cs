using System;
using NBitcoin.BouncyCastle.Math;
using NBitcoin.DataEncoders;
using Xunit;

namespace NBitcoin.Tests
{
    public class DeStreamNetworkTests
    {
        [Fact]
        [Trait("Category", "DeStream")]
        public void DeStreamMainIsInitializedCorrectly()
        {
            Network network = Network.DeStreamMain;

            Assert.Equal(2, network.Checkpoints.Count);
            Assert.Equal(2, network.DNSSeeds.Count);
            Assert.Equal(2, network.SeedNodes.Count);

            Assert.Equal("DeStreamMain", network.Name);
            Assert.Equal(Network.DeStreamRootFolderName, network.RootFolderName);
            Assert.Equal(Network.DeStreamDefaultConfigFilename, network.DefaultConfigFilename);
            Assert.Equal(0x10FEFE10.ToString(), network.Magic.ToString());
            Assert.Equal(56833, network.DefaultPort);
            Assert.Equal(56832, network.RPCPort);
            Assert.Equal(Network.DeStreamMaxTimeOffsetSeconds, network.MaxTimeOffsetSeconds);
            Assert.Equal(Network.DeStreamDefaultMaxTipAgeInSeconds, network.MaxTipAge);
            Assert.Equal(10000, network.MinTxFee);
            Assert.Equal(60000, network.FallbackFee);
            Assert.Equal(10000, network.MinRelayTxFee);
            Assert.Equal("DST", network.CoinTicker);

            Assert.Equal(2, network.Bech32Encoders.Length);
            Assert.Equal(new Bech32Encoder("bc").ToString(),
                network.Bech32Encoders[(int) Bech32Type.WITNESS_PUBKEY_ADDRESS].ToString());
            Assert.Equal(new Bech32Encoder("bc").ToString(),
                network.Bech32Encoders[(int) Bech32Type.WITNESS_SCRIPT_ADDRESS].ToString());

            Assert.Equal(12, network.Base58Prefixes.Length);
            Assert.Equal(new byte[] {30}, network.Base58Prefixes[(int) Base58Type.PUBKEY_ADDRESS]);
            Assert.Equal(new byte[] {90}, network.Base58Prefixes[(int) Base58Type.SCRIPT_ADDRESS]);
            Assert.Equal(new byte[] {30 + 90}, network.Base58Prefixes[(int) Base58Type.SECRET_KEY]);
            Assert.Equal(new byte[] {0x01, 0x42}, network.Base58Prefixes[(int) Base58Type.ENCRYPTED_SECRET_KEY_NO_EC]);
            Assert.Equal(new byte[] {0x01, 0x43}, network.Base58Prefixes[(int) Base58Type.ENCRYPTED_SECRET_KEY_EC]);
            Assert.Equal(new byte[] {0x04, 0x88, 0xB2, 0x1E}, network.Base58Prefixes[(int) Base58Type.EXT_PUBLIC_KEY]);
            Assert.Equal(new byte[] {0x04, 0x88, 0xAD, 0xE4}, network.Base58Prefixes[(int) Base58Type.EXT_SECRET_KEY]);
            Assert.Equal(new byte[] {0x2C, 0xE9, 0xB3, 0xE1, 0xFF, 0x39, 0xE2},
                network.Base58Prefixes[(int) Base58Type.PASSPHRASE_CODE]);
            Assert.Equal(new byte[] {0x64, 0x3B, 0xF6, 0xA8, 0x9A},
                network.Base58Prefixes[(int) Base58Type.CONFIRMATION_CODE]);
            Assert.Equal(new byte[] {0x2a}, network.Base58Prefixes[(int) Base58Type.STEALTH_ADDRESS]);
            Assert.Equal(new byte[] {23}, network.Base58Prefixes[(int) Base58Type.ASSET_ID]);
            Assert.Equal(new byte[] {0x13}, network.Base58Prefixes[(int) Base58Type.COLORED_ADDRESS]);

            Assert.Equal(210000, network.Consensus.SubsidyHalvingInterval);
            Assert.Equal(750, network.Consensus.MajorityEnforceBlockUpgrade);
            Assert.Equal(950, network.Consensus.MajorityRejectBlockOutdated);
            Assert.Equal(1000, network.Consensus.MajorityWindow);
            Assert.Equal(0, network.Consensus.BuriedDeployments[BuriedDeployments.BIP34]);
            Assert.Equal(0, network.Consensus.BuriedDeployments[BuriedDeployments.BIP65]);
            Assert.Equal(0, network.Consensus.BuriedDeployments[BuriedDeployments.BIP66]);
            Assert.Equal(new uint256("0x000000000000024b89b42a942fe0d9fea3bb44ab7bd1b19115dd6a759c0808b8"),
                network.Consensus.BIP34Hash);
            Assert.Equal(new Target(new uint256("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
                network.Consensus.PowLimit);
            Assert.Null(network.Consensus.MinimumChainWork);
            Assert.Equal(TimeSpan.FromSeconds(14 * 24 * 60 * 60), network.Consensus.PowTargetTimespan);
            Assert.Equal(TimeSpan.FromSeconds(10 * 60), network.Consensus.PowTargetSpacing);
            Assert.False(network.Consensus.PowAllowMinDifficultyBlocks);
            Assert.False(network.Consensus.PowNoRetargeting);
            Assert.Equal(1916, network.Consensus.RuleChangeActivationThreshold);
            Assert.Equal(2016, network.Consensus.MinerConfirmationWindow);
            Assert.Null(network.Consensus.BIP9Deployments[BIP9Deployments.TestDummy]);
            Assert.Null(network.Consensus.BIP9Deployments[BIP9Deployments.CSV]);
            Assert.Null(network.Consensus.BIP9Deployments[BIP9Deployments.Segwit]);
            Assert.Equal(1000, network.Consensus.LastPOWBlock);
            Assert.True(network.Consensus.IsProofOfStake);
            Assert.Equal(3564, network.Consensus.CoinType);
            Assert.Equal(
                new BigInteger(uint256.Parse("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")
                    .ToBytes(false)), network.Consensus.ProofOfStakeLimit);
            Assert.Equal(
                new BigInteger(uint256.Parse("000000000000ffffffffffffffffffffffffffffffffffffffffffffffffffff")
                    .ToBytes(false)), network.Consensus.ProofOfStakeLimitV2);
            Assert.Equal(new uint256("0x55a8205ae4bbf18f4d238c43f43005bd66e0b1f679b39e2c5c62cf6903693a5e"),
                network.Consensus.DefaultAssumeValid);
            Assert.Equal(50, network.Consensus.CoinbaseMaturity);
            Assert.Null(network.Consensus.PremineReward);
            Assert.Equal(0, network.Consensus.PremineHeight);
            Assert.Equal(Money.Zero, network.Consensus.ProofOfWorkReward);
            Assert.Equal(Money.Zero, network.Consensus.ProofOfStakeReward);
            Assert.Equal((uint) 500, network.Consensus.MaxReorgLength);
            Assert.Equal(long.MaxValue, network.Consensus.MaxMoney);

            Block genesis = network.GetGenesis();
            Assert.Equal(uint256.Parse("95dfb30e229e18197a812ece5d8d6c03efc9b9b65a9122a73f17d99613841b1b"),
                genesis.GetHash());
            Assert.Equal(uint256.Parse("6598d7cc968eae6d6e66e7ac88707f5e0948b816dc8ba52433d7edc1a1f2c6a3"),
                genesis.Header.HashMerkleRoot);
        }
    }
}
﻿using NBitcoin.DataEncoders;
using NBitcoin.Networks;

namespace NBitcoin
{
    public partial class Network
    {
        /// <summary>
        ///     The name of the root folder containing the different Stratis blockchains (StratisMain, StratisTest,
        ///     StratisRegTest).
        /// </summary>
        protected const string DeStreamRootFolderName = "destream";

        /// <summary> The default name used for the Stratis configuration file. </summary>
        protected const string DeStreamDefaultConfigFilename = "destream.conf";

        protected const int DeStreamMaxTimeOffsetSeconds = 25 * 60;

        protected const int DeStreamDefaultMaxTipAgeInSeconds = 2 * 60 * 60;


        public const string WalletAddressDeStreamMain = "TPPL2wmtxGzP8U6hQsGkRA9yCMsazB33ft";

        public static Network DeStreamMain => NetworksContainer.GetNetwork("DeStreamMain") ??
                                              NetworksContainer.Register(new DeStreamMain());

        public static Network DeStreamTest => NetworksContainer.GetNetwork("DeStreamTest") ??
                                              NetworksContainer.Register(new DeStreamTest());

        public static Network DeStreamRegTest => NetworksContainer.GetNetwork("DeStreamRegTest") ??
                                                 NetworksContainer.Register(new DeStreamRegTest());

        internal static Block CreateStratisGenesisBlock(ConsensusFactory consensusFactory, uint nTime, uint nNonce,
            uint nBits, int nVersion, Money genesisReward)
        {
            string pszTimestamp =
                "http://www.theonion.com/article/olympics-head-priestess-slits-throat-official-rio--53466";
            return CreateStratisGenesisBlock(consensusFactory, pszTimestamp, nTime, nNonce, nBits, nVersion,
                genesisReward);
        }

        private static Block CreateStratisGenesisBlock(ConsensusFactory consensusFactory, string pszTimestamp,
            uint nTime, uint nNonce, uint nBits, int nVersion, Money genesisReward)
        {
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
            txNew.AddOutput(new TxOut
            {
                Value = genesisReward
            });
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
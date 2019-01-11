using System;
using System.Collections.Generic;
using NBitcoin.DataEncoders;

namespace NBitcoin
{
    public abstract class DeStreamNetwork : Network
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

        protected LinkedList<string> DeStreamWallets;
        
        public string GenesisWalletAddress { get; protected set; }

        private LinkedListNode<string> DeStreamWalletsNode { get; set; }

        public string DeStreamWallet
        {
            get
            {
                DeStreamWalletsNode = DeStreamWalletsNode.NextOrFirst() ?? DeStreamWallets.First;
                return DeStreamWalletsNode.Value;
            }
        }

        /// <summary>
        /// </summary>
        public double DeStreamFeePart { get; set; }

        /// <summary>
        ///     Fee applied to all transactions
        /// </summary>
        public double FeeRate { get; set; }

        /// <summary>
        ///     Splits fee between miner and DeStream
        /// </summary>
        /// <param name="fee">Total amount of fees to be split</param>
        /// <param name="deStreamFee">DeStream fee part</param>
        /// <param name="minerReward">Miner fee part</param>
        public void SplitFee(long fee, out long deStreamFee, out long minerReward)
        {
            deStreamFee = Convert.ToInt64(fee * DeStreamFeePart);
            minerReward = fee - deStreamFee;
        }

        /// <summary>
        ///     Subtracts fee from sum of fees and transfer funds
        /// </summary>
        /// <param name="value">Sum of fees and transfer funds</param>
        /// <returns>Transfer funds without fees</returns>
        public Money SubtractFee(Money value)
        {
            return Convert.ToInt64(value.Satoshi / (1.0 + FeeRate));
        }

        public bool IsDeStreamAddress(string address)
        {
            return DeStreamWallets.Contains(address);
        }
    }
}
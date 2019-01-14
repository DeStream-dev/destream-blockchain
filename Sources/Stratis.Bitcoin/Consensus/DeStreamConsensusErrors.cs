namespace Stratis.Bitcoin.Consensus
{
    public static partial class ConsensusErrors
    {
        public static readonly ConsensusError BadBlockNoFeeOutput = new ConsensusError("bad-blk-feeoutput", "no fee output in coinbase or coinstake transactions");
        
        public static readonly ConsensusError BadBlockTotalFundsChanged = new ConsensusError("bad-block-total-funds-changed", "total amount of funds in blockchain changed");
    }
}
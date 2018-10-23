using Stratis.Bitcoin.Consensus;

namespace Stratis.Bitcoin.Features.Consensus
{
    public static partial class ConsensusErrors
    {
        public static readonly ConsensusError BadBlockNoFeeOutput = new ConsensusError("bad-blk-feeoutput", "no fee output in coinbase or coinstake transactions");
    }
}
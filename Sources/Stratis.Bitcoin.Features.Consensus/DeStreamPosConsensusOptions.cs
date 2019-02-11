using NBitcoin;
using Stratis.Bitcoin.Utilities;

namespace Stratis.Bitcoin.Features.Consensus
{
    public class DeStreamPosConsensusOptions : PosConsensusOptions
    {
        public override int GetStakeMinConfirmations(int height, Network network)
        {
            return network.IsTest() ? 20 : 500;
        }
    }
}
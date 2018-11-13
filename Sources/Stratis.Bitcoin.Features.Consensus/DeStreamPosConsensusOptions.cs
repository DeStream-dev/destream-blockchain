using NBitcoin;
using Stratis.Bitcoin.Utilities;

namespace Stratis.Bitcoin.Features.Consensus
{
    public class DeStreamPosConsensusOptions : PosConsensusOptions
    {
        public override int GetStakeMinConfirmations(int height, Network network)
        {
            if(network.IsTest())
                return height < CoinstakeMinConfirmationActivationHeightTestnet ? 10 : 20;

            return 500;
        }
    }
}
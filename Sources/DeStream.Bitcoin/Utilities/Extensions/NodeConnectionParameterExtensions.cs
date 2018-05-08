using DeStream.Bitcoin.P2P;
using DeStream.Bitcoin.P2P.Peer;

namespace DeStream.Bitcoin.Utilities.Extensions
{
    public static class NodeConnectionParameterExtensions
    {
        public static PeerAddressManagerBehaviour PeerAddressManagerBehaviour(this NetworkPeerConnectionParameters parameters)
        {
            return parameters.TemplateBehaviors.Find<PeerAddressManagerBehaviour>();
        }
    }
}
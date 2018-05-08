using System;
using NBitcoin;
using DeStream.Bitcoin.P2P.Peer;
using DeStream.Bitcoin.P2P.Protocol.Payloads;

namespace DeStream.Bitcoin.P2P.Protocol.Filters
{
    public class NetworkPeerFiltersCollection : ThreadSafeCollection<INetworkPeerFilter>
    {
        public IDisposable Add(Action<IncomingMessage, Action> onReceiving, Action<INetworkPeer, Payload, Action> onSending = null)
        {
            return base.Add(new ActionFilter(onReceiving, onSending));
        }
    }
}

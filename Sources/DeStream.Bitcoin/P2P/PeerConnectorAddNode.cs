using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NBitcoin;
using NBitcoin.Protocol;
using DeStream.Bitcoin.Configuration;
using DeStream.Bitcoin.Configuration.Settings;
using DeStream.Bitcoin.P2P.Peer;
using DeStream.Bitcoin.P2P.Protocol.Payloads;
using DeStream.Bitcoin.Utilities;
using DeStream.Bitcoin.Utilities.Extensions;

namespace DeStream.Bitcoin.P2P
{
    /// <summary>
    /// The connector used to connect to peers specified with the -addnode argument
    /// </summary>
    public sealed class PeerConnectorAddNode : PeerConnector
    {
        /// <summary>Constructor for dependency injection.</summary>
        public PeerConnectorAddNode(
            IAsyncLoopFactory asyncLoopFactory,
            IDateTimeProvider dateTimeProvider,
            ILoggerFactory loggerFactory,
            Network network,
            INetworkPeerFactory networkPeerFactory,
            INodeLifetime nodeLifetime,
            NodeSettings nodeSettings,
            ConnectionManagerSettings connectionSettings,
            IPeerAddressManager peerAddressManager,
            ISelfEndpointTracker selfEndpointTracker) :
            base(asyncLoopFactory, dateTimeProvider, loggerFactory, network, networkPeerFactory, nodeLifetime, nodeSettings, connectionSettings, peerAddressManager, selfEndpointTracker)
        {
            this.Requirements.RequiredServices = NetworkPeerServices.Nothing;
        }

        /// <inheritdoc/>
        public override void OnInitialize()
        {
            this.MaxOutboundConnections = this.ConnectionSettings.AddNode.Count;

            foreach (var ipEndpoint in this.ConnectionSettings.AddNode)
            {
                this.peerAddressManager.AddPeer(ipEndpoint.MapToIpv6(), IPAddress.Loopback);
            }
        }

        /// <summary>This connector is always started.</summary>
        public override bool CanStartConnect
        {
            get { return true; }
        }

        /// <inheritdoc/>
        public override void OnStartConnect()
        {
            this.CurrentParameters.PeerAddressManagerBehaviour().Mode = PeerAddressManagerBehaviourMode.AdvertiseDiscover;
        }

        /// <summary>
        /// Only connect to nodes as specified in the -addnode arg.
        /// </summary>
        public override async Task OnConnectAsync()
        {
            foreach (var ipEndpoint in this.ConnectionSettings.AddNode)
            {
                if (this.nodeLifetime.ApplicationStopping.IsCancellationRequested)
                    return;

                PeerAddress peerAddress = this.peerAddressManager.FindPeer(ipEndpoint);
                if (peerAddress != null && !this.IsPeerConnected(peerAddress.Endpoint))
                    await ConnectAsync(peerAddress).ConfigureAwait(false);
            }
        }
    }
}
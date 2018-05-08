using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NBitcoin;
using DeStream.Bitcoin.Connection;
using DeStream.Bitcoin.P2P.Peer;
using DeStream.Bitcoin.Utilities;

namespace DeStream.Bitcoin.Features.Wallet.Broadcasting
{
    public class LightWalletBroadcasterManager : BroadcasterManagerBase
    {
        public LightWalletBroadcasterManager(IConnectionManager connectionManager) : base(connectionManager)
        {
        }

        /// <inheritdoc />
        public override async Task BroadcastTransactionAsync(Transaction transaction)
        {
            Guard.NotNull(transaction, nameof(transaction));

            if (this.IsPropagated(transaction))
                return;

            List<INetworkPeer> peers = this.connectionManager.ConnectedPeers.ToList();
            int propagateToCount = (int)Math.Ceiling(peers.Count / 2.0);

            await this.PropagateTransactionToPeersAsync(transaction, peers.Take(propagateToCount).ToList()).ConfigureAwait(false);
        }
    }
}

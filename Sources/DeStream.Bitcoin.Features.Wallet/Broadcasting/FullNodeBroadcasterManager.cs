using System.Linq;
using System.Threading.Tasks;
using NBitcoin;
using DeStream.Bitcoin.Broadcasting;
using DeStream.Bitcoin.Connection;
using DeStream.Bitcoin.Features.MemoryPool;
using DeStream.Bitcoin.Utilities;

namespace DeStream.Bitcoin.Features.Wallet.Broadcasting
{
    public class FullNodeBroadcasterManager : BroadcasterManagerBase
    {
        /// <summary>Memory pool validator for validating transactions.</summary>
        private readonly IMempoolValidator mempoolValidator;

        public FullNodeBroadcasterManager(IConnectionManager connectionManager, IMempoolValidator mempoolValidator) : base(connectionManager)
        {
            Guard.NotNull(mempoolValidator, nameof(mempoolValidator));

            this.mempoolValidator = mempoolValidator;
        }

        /// <inheritdoc />
        public override async Task BroadcastTransactionAsync(Transaction transaction)
        {
            Guard.NotNull(transaction, nameof(transaction));

            if (this.IsPropagated(transaction))
                return;

            var state = new MempoolValidationState(false);
            if (!await this.mempoolValidator.AcceptToMemoryPool(state, transaction).ConfigureAwait(false))
                this.AddOrUpdate(transaction, State.CantBroadcast);
            else
                await this.PropagateTransactionToPeersAsync(transaction, this.connectionManager.ConnectedPeers.ToList()).ConfigureAwait(false);
        }
    }
}

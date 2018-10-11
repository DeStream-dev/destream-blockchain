using System.Linq;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Stratis.Bitcoin.Features.Wallet.Interfaces;

namespace Stratis.Bitcoin.Features.Wallet
{
    public class DeStreamWalletTransactionHandler : WalletTransactionHandler
    {
        public DeStreamWalletTransactionHandler(ILoggerFactory loggerFactory, IWalletManager walletManager,
            IWalletFeePolicy walletFeePolicy, Network network) : base(loggerFactory, walletManager, walletFeePolicy,
            network)
        {
        }

        /// <inheritdoc />
        protected override void AddFee(TransactionBuildContext context)
        {
            long fee = (long) (context.Recipients.Sum(p => p.Amount) * this.Network.FeeRate);
            context.TransactionFee = fee;
            context.TransactionBuilder.SendFees(fee);
        }
    }
}
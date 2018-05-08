using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NBitcoin;
using DeStream.Bitcoin.Features.RPC;
using DeStream.Bitcoin.Features.Wallet.Interfaces;

namespace DeStream.Bitcoin.Features.Wallet
{
    public class WalletRPCController : FeatureController
    {
        public WalletRPCController(IServiceProvider serviceProvider, IWalletManager walletManager)
        {
            this.WalletManager = walletManager;
            this.serviceProvider = serviceProvider;
        }

        internal IServiceProvider serviceProvider;

        public IWalletManager WalletManager { get; set; }

        [ActionName("sendtoaddress")]
        [ActionDescription("Sends money to a bitcoin address.")]
        public uint256 SendToAddress(BitcoinAddress bitcoinAddress, Money amount)
        {
            var account = this.GetAccount();
            return uint256.Zero;
        }

        private WalletAccountReference GetAccount()
        {
            //TODO: Support multi wallet like core by mapping passed RPC credentials to a wallet/account
            var w = this.WalletManager.GetWalletsNames().FirstOrDefault();
            if (w == null)
                throw new RPCServerException(NBitcoin.RPC.RPCErrorCode.RPC_INVALID_REQUEST, "No wallet found");
            var account = this.WalletManager.GetAccounts(w).FirstOrDefault();
            return new WalletAccountReference(w, account.Name);
        }
    }
}

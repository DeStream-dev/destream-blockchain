using System;

namespace DeStream.Bitcoin.Features.Wallet
{
    public class WalletException : Exception
    {
        public WalletException(string message) : base(message)
        {
        }
    }
}

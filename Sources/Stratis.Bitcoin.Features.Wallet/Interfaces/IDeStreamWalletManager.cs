namespace Stratis.Bitcoin.Features.Wallet.Interfaces
{
    public interface IDeStreamWalletManager : IWalletManager
    {
        /// <summary>
        ///     Processes genesis block
        /// </summary>
        void ProcessGenesisBlock();
    }
}
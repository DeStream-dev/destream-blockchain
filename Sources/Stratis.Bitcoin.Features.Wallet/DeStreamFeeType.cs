namespace Stratis.Bitcoin.Features.Wallet
{
    public enum DeStreamFeeType
    {
        /// <summary>
        /// Fee is charged from receiver
        /// </summary>
        Included,
        
        /// <summary>
        /// Fee is charged from sender
        /// </summary>
        Extra
    }
}
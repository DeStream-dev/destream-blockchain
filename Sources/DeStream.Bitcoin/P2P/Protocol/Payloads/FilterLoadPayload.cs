using NBitcoin;

namespace DeStream.Bitcoin.P2P.Protocol.Payloads
{
    /// <summary>
    /// Load a bloomfilter in the peer, used by SPV clients.
    /// </summary>
    [Payload("filterload")]
    public class FilterLoadPayload : BitcoinSerializablePayload<BloomFilter>
    {
        public FilterLoadPayload()
        {
        }

        public FilterLoadPayload(BloomFilter filter)
            : base(filter)
        {
        }
    }
}
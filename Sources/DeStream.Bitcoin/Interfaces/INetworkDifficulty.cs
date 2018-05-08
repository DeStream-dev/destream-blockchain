using NBitcoin;

namespace DeStream.Bitcoin.Interfaces
{
    public interface INetworkDifficulty
    {
        Target GetNetworkDifficulty();
    }
}

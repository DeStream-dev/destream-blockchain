using System.Text;

namespace DeStream.Bitcoin.Interfaces
{
    public interface IFeatureStats
    {
        void AddFeatureStats(StringBuilder benchLog);
    }
}

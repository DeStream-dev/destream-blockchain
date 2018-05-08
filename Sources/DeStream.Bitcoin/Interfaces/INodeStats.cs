using System.Text;

namespace DeStream.Bitcoin.Interfaces
{
    public interface INodeStats
    {
        void AddNodeStats(StringBuilder benchLog);
    }
}

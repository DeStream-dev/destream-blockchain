using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace NBitcoin.RPC
{
    public class AddedNodeInfo
    {
        public EndPoint AddedNode { get; internal set; }
        public bool Connected { get; internal set; }
        public IEnumerable<NodeAddressInfo> Addresses { get; internal set; }
    }
}

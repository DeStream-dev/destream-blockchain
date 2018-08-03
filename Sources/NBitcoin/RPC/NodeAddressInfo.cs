using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace NBitcoin.RPC
{
    public class NodeAddressInfo
    {
        public IPEndPoint Address { get; internal set; }
        public bool Connected { get; internal set; }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace NBitcoin.RPC
{
    public class RPCAccount
    {
        public Money Amount { get; set; }
        public string AccountName { get; set; }
    }
}

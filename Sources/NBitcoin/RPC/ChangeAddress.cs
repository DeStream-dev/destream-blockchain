using System;
using System.Collections.Generic;
using System.Text;

namespace NBitcoin.RPC
{
    public class ChangeAddress
    {
        public Money Amount { get; set; }
        public BitcoinAddress Address { get; set; }
    }
}

using System;
using NBitcoin.RPC;

namespace DeStream.Bitcoin.Features.RPC
{
    public class RPCServerException : Exception
    {
        public RPCServerException(RPCErrorCode errorCode, string message) : base(message)
        {
            this.ErrorCode = errorCode;
        }

        public RPCErrorCode ErrorCode { get; set; }
    }
}

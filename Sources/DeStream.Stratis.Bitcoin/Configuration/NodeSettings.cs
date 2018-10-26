using System;
using System.IO;
using Microsoft.Extensions.Logging;
using NBitcoin;
using NBitcoin.Protocol;
using Stratis.Bitcoin.Configuration.Logging;
using Stratis.Bitcoin.Utilities;
using Stratis.Bitcoin.Configuration;
using System.Collections.Generic;
using Stratis.Bitcoin.Builder.Feature;
using Stratis.Bitcoin.Utilities.Extensions;

namespace DeStream.Stratis.Bitcoin.Configuration
{
    /// <summary>
    /// Node configuration complied from both the application command line arguments and the configuration file.
    /// </summary>
    public class DeStreamNodeSettings : NodeSettings
    {
        /// <summary>
        /// Returns default data root directory name
        /// </summary>
        protected override string DataRootDirName
        {
            get { return "DeStreamNode"; }
        }

        /// <summary>
        /// Initializes a new instance of the object.
        /// </summary>
        /// <param name="innerNetwork">Specification of the network the node runs on - regtest/testnet/mainnet.</param>
        /// <param name="protocolVersion">Supported protocol version for which to create the configuration.</param>
        /// <param name="agent">The nodes user agent that will be shared with peers.</param>
        public DeStreamNodeSettings(Network innerNetwork = null, ProtocolVersion protocolVersion = SupportedProtocolVersion, 
            string agent = "DeStream", string[] args = null, bool loadConfiguration = true) 
            : base (innerNetwork, protocolVersion, agent, args)
        {            
        }
    }
}
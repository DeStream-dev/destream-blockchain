using System;
using System.IO;
using Microsoft.Extensions.Logging;
using NBitcoin;
using NBitcoin.Protocol;
using Stratis.Bitcoin.Configuration.Logging;
using Stratis.Bitcoin.Utilities;
using Stratis.Bitcoin.Configuration;

namespace DeStream.Stratis.Bitcoin.Configuration
{
    /// <summary>
    /// Node configuration complied from both the application command line arguments and the configuration file.
    /// </summary>
    public class DeStreamNodeSettings : NodeSettings
    {
        /// <summary>
        /// Initializes a new instance of the object.
        /// </summary>
        /// <param name="innerNetwork">Specification of the network the node runs on - regtest/testnet/mainnet.</param>
        /// <param name="protocolVersion">Supported protocol version for which to create the configuration.</param>
        /// <param name="agent">The nodes user agent that will be shared with peers.</param>
        public DeStreamNodeSettings(Network innerNetwork = null, ProtocolVersion protocolVersion = SupportedProtocolVersion, 
            string agent = "DeStream", string[] args = null, bool loadConfiguration = true) 
            : base (innerNetwork, protocolVersion, agent, args, loadConfiguration)
        {
        }

        /// <summary>
        /// Loads the configuration file.
        /// </summary>
        /// <returns>Initialized node configuration.</returns>
        /// <exception cref="ConfigurationException">Thrown in case of any problems with the configuration file or command line arguments.</exception>
        public override NodeSettings LoadConfiguration()
        {
            // Configuration already loaded?
            if (this.ConfigReader != null)
                return this;

            // Get the arguments set previously
            var args = this.LoadArgs;

            // Setting the data directory.
            if (this.DataDir == null)
            {
                this.DataDir = this.CreateDefaultDataDirectories(Path.Combine("DeStreamNode", this.Network.RootFolderName), this.Network);
            }
            else
            {
                // Create the data directories if they don't exist.
                string directoryPath = Path.Combine(this.DataDir, this.Network.RootFolderName, this.Network.Name);
                Directory.CreateDirectory(directoryPath);
                this.DataDir = directoryPath;
                this.Logger.LogDebug("Data directory initialized with path {0}.", directoryPath);
            }

            // If no configuration file path is passed in the args, load the default file.
            if (this.ConfigurationFile == null)
            {
                this.ConfigurationFile = this.CreateDefaultConfigurationFile();
            }

            var consoleConfig = new TextFileConfiguration(args);
            var config = new TextFileConfiguration(File.ReadAllText(this.ConfigurationFile));
            this.ConfigReader = config;
            consoleConfig.MergeInto(config);

            this.DataFolder = new DataFolder(this.DataDir);
            if (!Directory.Exists(this.DataFolder.CoinViewPath))
                Directory.CreateDirectory(this.DataFolder.CoinViewPath);

            // Set the configuration filter and file path.
            this.Log.Load(config);
            this.LoggerFactory.AddFilters(this.Log, this.DataFolder);
            this.LoggerFactory.ConfigureConsoleFilters(this.LoggerFactory.GetConsoleSettings(), this.Log);

            this.Logger.LogDebug("Data directory set to '{0}'.", this.DataDir);
            this.Logger.LogDebug("Configuration file set to '{0}'.", this.ConfigurationFile);

            this.RequireStandard = config.GetOrDefault("acceptnonstdtxn", !(this.Network.IsTest()));
            this.MaxTipAge = config.GetOrDefault("maxtipage", this.Network.MaxTipAge);
            this.Logger.LogDebug("Network: IsTest='{0}', IsBitcoin='{1}'.", this.Network.IsTest(), this.Network.IsBitcoin());
            this.MinTxFeeRate = new FeeRate(config.GetOrDefault("mintxfee", this.Network.MinTxFee));
            this.Logger.LogDebug("MinTxFeeRate set to {0}.", this.MinTxFeeRate);
            this.FallbackTxFeeRate = new FeeRate(config.GetOrDefault("fallbackfee", this.Network.FallbackFee));
            this.Logger.LogDebug("FallbackTxFeeRate set to {0}.", this.FallbackTxFeeRate);
            this.MinRelayTxFeeRate = new FeeRate(config.GetOrDefault("minrelaytxfee", this.Network.MinRelayTxFee));
            this.Logger.LogDebug("MinRelayTxFeeRate set to {0}.", this.MinRelayTxFeeRate);
            this.SyncTimeEnabled = config.GetOrDefault<bool>("synctime", true);
            this.Logger.LogDebug("Time synchronization with peers is {0}.", this.SyncTimeEnabled ? "enabled" : "disabled");

            // Add a prefix set by the user to the agent. This will allow people running nodes to
            // identify themselves if they wish. The prefix is limited to 10 characters.
            string agentPrefix = config.GetOrDefault("agentprefix", string.Empty);
            agentPrefix = agentPrefix.Substring(0, Math.Min(10, agentPrefix.Length));
            this.Agent = string.IsNullOrEmpty(agentPrefix) ? this.Agent : $"{agentPrefix}-{this.Agent}"; 

            return this;
        }
    }
}
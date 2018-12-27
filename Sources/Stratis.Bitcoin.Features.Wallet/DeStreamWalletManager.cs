using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Stratis.Bitcoin.Configuration;
using Stratis.Bitcoin.Features.Wallet.Interfaces;
using Stratis.Bitcoin.Utilities;

namespace Stratis.Bitcoin.Features.Wallet
{
    /// <inheritdoc cref="WalletManager" />
    public class DeStreamWalletManager : WalletManager, IDeStreamWalletManager
    {
        public DeStreamWalletManager(ILoggerFactory loggerFactory, Network network, ConcurrentChain chain,
            NodeSettings settings, WalletSettings walletSettings,
            DataFolder dataFolder, IWalletFeePolicy walletFeePolicy, IAsyncLoopFactory asyncLoopFactory,
            INodeLifetime nodeLifetime, IDateTimeProvider dateTimeProvider,
            IBroadcasterManager broadcasterManager = null) :
            base(loggerFactory, network, chain, settings, walletSettings, dataFolder, walletFeePolicy, asyncLoopFactory,
                nodeLifetime, dateTimeProvider, broadcasterManager)
        {
        }

        /// <inheritdoc />
        public override Wallet LoadWallet(string password, string name)
        {
            Wallet result = base.LoadWallet(password, name);

            this.LoadKeysLookupLock();

            return result;
        }

        /// <inheritdoc />
        public void ProcessGenesisBlock()
        {
            foreach (var transactionWithOutput in this.network.GetGenesis().Transactions.SelectMany(p =>
                p.Outputs.Select(q => new {Transaction = p, Output = q}).Where(q =>
                    this.keysLookup.TryGetValue(q.Output.ScriptPubKey, out HdAddress _))))
            {
                this.AddTransactionToWallet(transactionWithOutput.Transaction, transactionWithOutput.Output, 0,
                    this.network.GetGenesis());
            }
        }

        protected override void AddSpendingTransactionToWallet(Transaction transaction,
            IEnumerable<TxOut> paidToOutputs, uint256 spendingTransactionId,
            int? spendingTransactionIndex, int? blockHeight = null, Block block = null)
        {
            Guard.NotNull(transaction, nameof(transaction));
            Guard.NotNull(paidToOutputs, nameof(paidToOutputs));

            this.logger.LogTrace("({0}:'{1}',{2}:'{3}',{4}:{5},{6}:'{7}')", nameof(transaction), transaction.GetHash(),
                nameof(spendingTransactionId), spendingTransactionId, nameof(spendingTransactionIndex),
                spendingTransactionIndex, nameof(blockHeight), blockHeight);

            // Get the transaction being spent.
            TransactionData spentTransaction = this.keysLookup.Values.Distinct().SelectMany(v => v.Transactions)
                .SingleOrDefault(t => t.Id == spendingTransactionId && t.Index == spendingTransactionIndex);
            if (spentTransaction == null)
            {
                // Strange, why would it be null?
                this.logger.LogTrace("(-)[TX_NULL]");
                return;
            }
                this.logger.LogTrace(spentTransaction.SpendingDetails == null
                    ? $"Spending UTXO '{spendingTransactionId}-{spendingTransactionIndex}' is new."
                    : $"Spending transaction ID '{spendingTransactionId}' is being confirmed, updating.");

            var payments = new List<PaymentDetails>();
            foreach (TxOut paidToOutput in paidToOutputs)
            {
                // Figure out how to retrieve the destination address.
                string destinationAddress = string.Empty;
                ScriptTemplate scriptTemplate = paidToOutput.ScriptPubKey.FindTemplate(this.network);
                switch (scriptTemplate.Type)
                {
                    // Pay to PubKey can be found in outputs of staking transactions.
                    case TxOutType.TX_PUBKEY:
                        PubKey pubKey =
                            PayToPubkeyTemplate.Instance.ExtractScriptPubKeyParameters(paidToOutput.ScriptPubKey);
                        destinationAddress = pubKey.GetAddress(this.network).ToString();
                        break;
                    // Pay to PubKey hash is the regular, most common type of output.
                    case TxOutType.TX_PUBKEYHASH:
                        destinationAddress = paidToOutput.ScriptPubKey.GetDestinationAddress(this.network).ToString();
                        break;
                    case TxOutType.TX_NONSTANDARD:
                    case TxOutType.TX_SCRIPTHASH:
                    case TxOutType.TX_MULTISIG:
                    case TxOutType.TX_NULL_DATA:
                    case TxOutType.TX_SEGWIT:
                        break;
                }

                payments.Add(new PaymentDetails
                {
                    DestinationScriptPubKey = paidToOutput.ScriptPubKey,
                    DestinationAddress = destinationAddress,
                    Amount = paidToOutput.Value
                });
            }

            var spendingDetails = new SpendingDetails
            {
                TransactionId = transaction.GetHash(),
                Payments = payments,
                CreationTime = DateTimeOffset.FromUnixTimeSeconds(block?.Header.Time ?? transaction.Time),
                BlockHeight = blockHeight,
                Hex = this.walletSettings.SaveTransactionHex ? transaction.ToHex() : null,
                IsCoinStake = transaction.IsCoinStake == false ? (bool?) null : true
            };

            spentTransaction.SpendingDetails = spendingDetails;
            spentTransaction.MerkleProof = null;

            this.logger.LogTrace("(-)");
        }
    }
}
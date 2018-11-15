using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Stratis.Bitcoin.Features.Consensus;
using Stratis.Bitcoin.Features.Wallet.Interfaces;
using Stratis.Bitcoin.Utilities;
using Stratis.Bitcoin.Utilities.Extensions;

namespace Stratis.Bitcoin.Features.Wallet
{
    public class DeStreamWalletTransactionHandler : WalletTransactionHandler
    {
        public DeStreamWalletTransactionHandler(ILoggerFactory loggerFactory, IWalletManager walletManager,
            IWalletFeePolicy walletFeePolicy, Network network) : base(loggerFactory, walletManager, walletFeePolicy,
            network)
        {
        }

        /// <inheritdoc />
        protected override void AddFee(TransactionBuildContext context)
        {
            long fee = Convert.ToInt64(context.Recipients.Sum(p => p.Amount) * this.Network.FeeRate);
            context.TransactionFee = fee;
            context.TransactionBuilder.SendFees(fee);
        }

        /// <inheritdoc />
        public override (Money maximumSpendableAmount, Money Fee) GetMaximumSpendableAmount(
            WalletAccountReference accountReference,
            FeeType feeType, bool allowUnconfirmed)
        {
            Guard.NotNull(accountReference, nameof(accountReference));
            Guard.NotEmpty(accountReference.WalletName, nameof(accountReference.WalletName));
            Guard.NotEmpty(accountReference.AccountName, nameof(accountReference.AccountName));

            // Get the total value of spendable coins in the account.
            long maxSpendableAmount = this.walletManager
                .GetSpendableTransactionsInAccount(accountReference, allowUnconfirmed ? 0 : 1)
                .Sum(x => x.Transaction.Amount);

            // Return 0 if the user has nothing to spend.
            if (maxSpendableAmount == Money.Zero) return (Money.Zero, Money.Zero);

            // Create a recipient with a dummy destination address as it's required by NBitcoin's transaction builder.
            List<Recipient> recipients = new[]
                    {new Recipient {Amount = new Money(maxSpendableAmount), ScriptPubKey = new Key().ScriptPubKey}}
                .ToList();
            Money fee;

            try
            {
                // Here we try to create a transaction that contains all the spendable coins, leaving no room for the fee.
                // When the transaction builder throws an exception informing us that we have insufficient funds,
                // we use the amount we're missing as the fee.
                var context = new TransactionBuildContext(accountReference, recipients, null)
                {
                    FeeType = feeType,
                    MinConfirmations = allowUnconfirmed ? 0 : 1,
                    TransactionBuilder = new DeStreamTransactionBuilder(this.Network)
                };

                this.AddRecipients(context);
                this.AddCoins(context);
                this.AddFee(context);

                // Throw an exception if this code is reached, as building a transaction without any funds for the fee should always throw an exception.
                throw new WalletException(
                    "This should be unreachable; please find and fix the bug that caused this to be reached.");
            }
            catch (NotEnoughFundsException e)
            {
                fee = (Money) e.Missing;
            }

            return (maxSpendableAmount - fee, fee);
        }

        /// <inheritdoc />
        protected override void AddSecrets(TransactionBuildContext context)
        {
            if (!context.Sign)
                return;

            Wallet wallet = this.walletManager.GetWalletByName(context.AccountReference.WalletName);
            Key privateKey;
            // get extended private key
            string cacheKey = wallet.EncryptedSeed;

            if (this.privateKeyCache.TryGetValue(cacheKey, out SecureString secretValue))
            {
                privateKey = wallet.Network.CreateBitcoinSecret(secretValue.FromSecureString()).PrivateKey;
                this.privateKeyCache.Set(cacheKey, secretValue, new TimeSpan(0, 5, 0));
            }
            else
            {
                privateKey = Key.Parse(wallet.EncryptedSeed, context.WalletPassword, wallet.Network);
                this.privateKeyCache.Set(cacheKey, privateKey.ToString(wallet.Network).ToSecureString(),
                    new TimeSpan(0, 5, 0));
            }

            var seedExtKey = new ExtKey(privateKey, wallet.ChainCode);

            var signingKeys = new HashSet<ISecret>();
            var added = new HashSet<HdAddress>();
            foreach (UnspentOutputReference unspentOutputsItem in context.UnspentOutputs)
            {
                if (added.Contains(unspentOutputsItem.Address))
                    continue;

                HdAddress address = unspentOutputsItem.Address;
                ExtKey addressExtKey = seedExtKey.Derive(new KeyPath(address.HdPath));
                BitcoinExtKey addressPrivateKey = addressExtKey.GetWif(wallet.Network);
                signingKeys.Add(addressPrivateKey);
                added.Add(unspentOutputsItem.Address);
            }

            // To secure that fee is charged from spending coins and not from change,
            // we add empty input with change address, so private key for change address is required.
            BitcoinExtKey changeAddressPrivateKey =
                seedExtKey.Derive(new KeyPath(context.ChangeAddress.HdPath)).GetWif(wallet.Network);
            signingKeys.Add(changeAddressPrivateKey);

            context.TransactionBuilder.AddKeys(signingKeys.ToArray());
        }

        /// <inheritdoc />
        protected override void AddCoins(TransactionBuildContext context)
        {
            // Transaction.IsCoinStake == true - for coinstake transactions
            // Transaction.IsCoinStake == null - for non-coinstake transactions
            // Transaction.IsCoinStake == false - not encountered (bug?)

            // Take all non-coinstake transactions
            context.UnspentOutputs = this.walletManager
                .GetSpendableTransactionsInAccount(context.AccountReference, context.MinConfirmations)
                .Where(p => p.Transaction.IsCoinStake != true)
                .ToList();

            // Add all coinstake transactions with enough confirmations
            context.UnspentOutputs.AddRange(this.walletManager
                .GetSpendableTransactionsInAccount(context.AccountReference,
                    this.Network.Consensus.Option<PosConsensusOptions>()
                        .GetStakeMinConfirmations(this.walletManager.LastBlockHeight(), this.Network))
                .Where(p => p.Transaction.IsCoinStake ?? false)
                .ToList());

            (context.TransactionBuilder.CoinSelector as DeStreamCoinSelector ?? throw new NotSupportedException(
                 $"{nameof(context.TransactionBuilder.CoinSelector)} must be {typeof(DeStreamCoinSelector)} type"))
                .StakeScript = context.UnspentOutputs
                    .FirstOrDefault(p => p.Transaction.IsCoinStake ?? false)
                    ?.Address.Pubkey;

            if (context.UnspentOutputs.Count == 0) throw new WalletException("No spendable transactions found.");

            // Get total spendable balance in the account.
            long balance = context.UnspentOutputs.Sum(t => t.Transaction.Amount);
            long totalToSend = context.Recipients.Sum(s => s.Amount);
            if (balance < totalToSend)
                throw new WalletException("Not enough funds.");

            if (context.SelectedInputs.Any())
            {
                // 'SelectedInputs' are inputs that must be included in the
                // current transaction. At this point we check the given
                // input is part of the UTXO set and filter out UTXOs that are not
                // in the initial list if 'context.AllowOtherInputs' is false.

                Dictionary<OutPoint, UnspentOutputReference> availableHashList =
                    context.UnspentOutputs.ToDictionary(item => item.ToOutPoint(), item => item);

                if (!context.SelectedInputs.All(input => availableHashList.ContainsKey(input)))
                    throw new WalletException("Not all the selected inputs were found on the wallet.");

                if (!context.AllowOtherInputs)
                {
                    foreach (KeyValuePair<OutPoint, UnspentOutputReference> unspentOutputsItem in availableHashList)
                    {
                        if (!context.SelectedInputs.Contains(unspentOutputsItem.Key))
                            context.UnspentOutputs.Remove(unspentOutputsItem.Value);
                    }
                }
            }

            Money sum = 0;
            int index = 0;
            var coins = new List<Coin>();
            foreach (UnspentOutputReference item in context.UnspentOutputs.OrderByDescending(a => a.Transaction.Amount))
            {
                coins.Add(new Coin(item.Transaction.Id, (uint) item.Transaction.Index, item.Transaction.Amount,
                    item.Transaction.ScriptPubKey));
                sum += item.Transaction.Amount;
                index++;

                // If threshold is reached and the total value is above the target
                // then its safe to stop adding UTXOs to the coin list.
                // The primery goal is to reduce the time it takes to build a trx
                // when the wallet is bloated with UTXOs.
                if (index > SendCountThresholdLimit && sum > totalToSend)
                    break;
            }

            // All the UTXOs are added to the builder without filtering.
            // The builder then has its own coin selection mechanism
            // to select the best UTXO set for the corresponding amount.
            // To add a custom implementation of a coin selection override
            // the builder using builder.SetCoinSelection().

            context.TransactionBuilder.AddCoins(coins);
        }

        /// <inheritdoc />
        protected override void InitializeTransactionBuilder(TransactionBuildContext context)
        {
            Guard.NotNull(context, nameof(context));
            Guard.NotNull(context.Recipients, nameof(context.Recipients));
            Guard.NotNull(context.AccountReference, nameof(context.AccountReference));

            context.TransactionBuilder = new DeStreamTransactionBuilder(this.Network);

            this.AddRecipients(context);
            this.AddOpReturnOutput(context);
            this.AddCoins(context);
            this.FindChangeAddress(context);
            this.AddSecrets(context);
            this.AddFee(context);
        }
    }
}
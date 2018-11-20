using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Stratis.Bitcoin.Features.Wallet.Interfaces;
using Stratis.Bitcoin.Features.Wallet.Models;
using Stratis.Bitcoin.Utilities;
using Stratis.Bitcoin.Utilities.JsonErrors;

namespace Stratis.Bitcoin.Features.Wallet.Controllers
{
    /// <summary>
    ///     Controller providing operations on a wallet.
    /// </summary>
    [Route("api/[controller]")]
    public class DeStreamWalletController : Controller
    {
        private readonly ILogger _logger;
        private readonly Network _network;
        private readonly IWalletTransactionHandler _walletTransactionHandler;

        public DeStreamWalletController(Network network, IWalletTransactionHandler walletTransactionHandler,
            ILoggerFactory loggerFactory)
        {
            this._network = network;
            this._walletTransactionHandler = walletTransactionHandler;
            this._logger = loggerFactory.CreateLogger(this.GetType().FullName);
        }

        /// <summary>
        ///     Builds a transaction.
        /// </summary>
        /// <param name="request">The transaction parameters.</param>
        /// <returns>All the details of the transaction, including the hex used to execute it.</returns>
        [Route("build-transaction")]
        [HttpPost]
        public IActionResult BuildTransaction([FromBody] BuildTransactionRequest request)
        {
            Guard.NotNull(request, nameof(request));

            // checks the request is valid
            if (!this.ModelState.IsValid) return WalletController.BuildErrorResponse(this.ModelState);

            try
            {
                Script destination = BitcoinAddress.Create(request.DestinationAddress, this._network).ScriptPubKey;
                var context = new TransactionBuildContext(
                    new WalletAccountReference(request.WalletName, request.AccountName),
                    new[] {new Recipient {Amount = request.Amount, ScriptPubKey = destination}}.ToList(),
                    request.Password, request.OpReturnData)
                {
                    TransactionFee = string.IsNullOrEmpty(request.FeeAmount) ? null : Money.Parse(request.FeeAmount),
                    MinConfirmations = request.AllowUnconfirmed ? 0 : 1,
                    Shuffle =
                        request.ShuffleOutputs ??
                        true // We shuffle transaction outputs by default as it's better for anonymity.
                };

                this.ProcessFeeType(request.FeeType, context.Recipients);

                Transaction transactionResult = this._walletTransactionHandler.BuildTransaction(context);

                var model = new WalletBuildTransactionModel
                {
                    Hex = transactionResult.ToHex(),
                    Fee = context.TransactionFee,
                    TransactionId = transactionResult.GetHash()
                };

                return this.Json(model);
            }
            catch (Exception e)
            {
                this._logger.LogError("Exception occurred: {0}", e.ToString());
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, e.Message, e.ToString());
            }
        }

        /// <summary>
        ///     Gets a transaction fee estimate.
        ///     Fee can be estimated by creating a <see cref="TransactionBuildContext" /> with no password
        ///     and then building the transaction and retrieving the fee from the context.
        /// </summary>
        /// <param name="request">The transaction parameters.</param>
        /// <returns>The estimated fee for the transaction.</returns>
        [Route("estimate-txfee")]
        [HttpGet]
        public IActionResult GetTransactionFeeEstimate([FromQuery] TxFeeEstimateRequest request)
        {
            Guard.NotNull(request, nameof(request));

            // checks the request is valid
            if (!this.ModelState.IsValid) return WalletController.BuildErrorResponse(this.ModelState);

            try
            {
                Script destination = BitcoinAddress.Create(request.DestinationAddress, this._network).ScriptPubKey;
                var context = new TransactionBuildContext(
                    new WalletAccountReference(request.WalletName, request.AccountName),
                    new[] {new Recipient {Amount = request.Amount, ScriptPubKey = destination}}.ToList())
                {
                    MinConfirmations = request.AllowUnconfirmed ? 0 : 1
                };

                this.ProcessFeeType(request.FeeType, context.Recipients);

                return this.Json(this._walletTransactionHandler.EstimateFee(context));
            }
            catch (Exception e)
            {
                this._logger.LogError("Exception occurred: {0}", e.ToString());
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, e.Message, e.ToString());
            }
        }

        /// <summary>
        ///     Modifies transfer funds amount according to fee type.
        ///     For <see cref="DeStreamFeeType.Extra" /> fee type, fee is subtracted from transfer funds.
        ///     For <see cref="DeStreamFeeType.Included" /> fee type, nothing changed.
        /// </summary>
        /// <param name="requestFeeType">Fee type in request</param>
        /// <param name="recipients">Recipients</param>
        /// <exception cref="FormatException">Fee type string is invalid</exception>
        private void ProcessFeeType(string requestFeeType, IEnumerable<Recipient> recipients)
        {
            if (Enum.TryParse(requestFeeType, out DeStreamFeeType feeType))
            {
                if (feeType != DeStreamFeeType.Included) return;

                foreach (Recipient recipient in recipients)
                    recipient.Amount = this._network.SubtractFee(recipient.Amount);
            }
            else
                throw new FormatException($"FeeType {requestFeeType} is not a valid DeStreamFeeType");
        }
    }
}
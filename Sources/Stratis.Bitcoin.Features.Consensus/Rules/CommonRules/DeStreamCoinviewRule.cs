using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Stratis.Bitcoin.Base.Deployments;
using Stratis.Bitcoin.Consensus.Rules;
using Stratis.Bitcoin.Utilities;

namespace Stratis.Bitcoin.Features.Consensus.Rules.CommonRules
{
    /// <summary>
    /// CoinViewRule that prevents verifing ChangePointer input
    /// </summary>
    public abstract class DeStreamCoinViewRule : CoinViewRule
    {
        public override async Task RunAsync(RuleContext context)
        {
            this.Logger.LogTrace("()");

            Block block = context.ValidationContext.Block;
            ChainedHeader index = context.ValidationContext.ChainedHeader;
            DeploymentFlags flags = context.Flags;
            UnspentOutputSet view = (context as UtxoRuleContext).UnspentOutputSet;

            this.Parent.PerformanceCounter.AddProcessedBlocks(1);

            long sigOpsCost = 0;
            Money fees = Money.Zero;
            var checkInputs = new List<Task<bool>>();
            for (int txIndex = 0; txIndex < block.Transactions.Count; txIndex++)
            {
                this.Parent.PerformanceCounter.AddProcessedTransactions(1);
                Transaction tx = block.Transactions[txIndex];
                if (!context.SkipValidation)
                {
                    if (!this.IsProtocolTransaction(tx))
                    {
                        if (!view.HaveInputs(tx))
                        {
                            this.Logger.LogTrace("(-)[BAD_TX_NO_INPUT]");
                            ConsensusErrors.BadTransactionMissingInput.Throw();
                        }

                        var prevheights = new int[tx.Inputs.Count];
                        // Check that transaction is BIP68 final.
                        // BIP68 lock checks (as opposed to nLockTime checks) must
                        // be in ConnectBlock because they require the UTXO set.
                        for (int j = 0; j < tx.Inputs.Count; j++)
                        {
                            prevheights[j] = tx.Inputs[j].IsChangePointer()
                                ? 0
                                : (int) view.AccessCoins(tx.Inputs[j].PrevOut.Hash).Height;
                        }

                        if (!tx.CheckSequenceLocks(prevheights, index, flags.LockTimeFlags))
                        {
                            this.Logger.LogTrace("(-)[BAD_TX_NON_FINAL]");
                            ConsensusErrors.BadTransactionNonFinal.Throw();
                        }
                    }

                    // GetTransactionSignatureOperationCost counts 3 types of sigops:
                    // * legacy (always),
                    // * p2sh (when P2SH enabled in flags and excludes coinbase),
                    // * witness (when witness enabled in flags and excludes coinbase).
                    sigOpsCost += this.GetTransactionSignatureOperationCost(tx, view, flags);
                    if (sigOpsCost > this.PowConsensusOptions.MaxBlockSigopsCost)
                    {
                        this.Logger.LogTrace("(-)[BAD_BLOCK_SIG_OPS]");
                        ConsensusErrors.BadBlockSigOps.Throw();
                    }

                    if (!this.IsProtocolTransaction(tx))
                    {
                        this.CheckInputs(tx, view, index.Height);
                        fees += view.GetValueIn(tx) - tx.TotalOut;
                        var txData = new PrecomputedTransactionData(tx);
                        for (int inputIndex = 0; inputIndex < tx.Inputs.Count; inputIndex++)
                        {
                            this.Parent.PerformanceCounter.AddProcessedInputs(1);
                            TxIn input = tx.Inputs[inputIndex];
                            int inputIndexCopy = inputIndex;
                            TxOut txout = input.IsChangePointer()
                                ? tx.Outputs[input.PrevOut.N]
                                : view.GetOutputFor(input);
                            var checkInput = new Task<bool>(() =>
                            {
                                var checker = new TransactionChecker(tx, inputIndexCopy, txout.Value, txData);
                                var ctx = new ScriptEvaluationContext(this.Parent.Network);
                                ctx.ScriptVerify = flags.ScriptFlags;
                                bool verifyScriptResult =
                                    ctx.VerifyScript(input.ScriptSig, txout.ScriptPubKey, checker);

                                if (verifyScriptResult == false)
                                    this.Logger.LogTrace(
                                        "Verify script for transaction '{0}' failed, ScriptSig = '{1}', ScriptPubKey = '{2}', script evaluation error = '{3}'",
                                        tx.GetHash(), input.ScriptSig, txout.ScriptPubKey, ctx.Error);

                                return verifyScriptResult;
                            });
                            checkInput.Start();
                            checkInputs.Add(checkInput);
                        }
                    }
                }

                this.UpdateCoinView(context, tx);
            }

            if (!context.SkipValidation)
            {
                this.CheckBlockReward(context, fees, index.Height, block);

                foreach (Task<bool> checkInput in checkInputs)
                {
                    if (await checkInput.ConfigureAwait(false))
                        continue;

                    this.Logger.LogTrace("(-)[BAD_TX_SCRIPT]");
                    ConsensusErrors.BadTransactionScriptError.Throw();
                }
            }
            else
                this.Logger.LogTrace("BIP68, SigOp cost, and block reward validation skipped for block at height {0}.",
                    index.Height);

            this.Logger.LogTrace("(-)");
        }

        /// <inheritdoc />
        public override void CheckInputs(Transaction transaction, UnspentOutputSet inputs, int spendHeight)
        {
            this.Logger.LogTrace("({0}:{1})", nameof(spendHeight), spendHeight);

            if (!inputs.HaveInputs(transaction))
                ConsensusErrors.BadTransactionMissingInput.Throw();

            Money valueIn = Money.Zero;
            Money fees = Money.Zero;
            foreach (TxIn txIn in transaction.Inputs.RemoveChangePointer())
            {
                OutPoint prevout = txIn.PrevOut;
                UnspentOutputs coins = inputs.AccessCoins(prevout.Hash);

                this.CheckMaturity(coins, spendHeight);

                // Check for negative or overflow input values.
                valueIn += coins.TryGetOutput(prevout.N).Value;
                if (!this.MoneyRange(coins.TryGetOutput(prevout.N).Value) || !this.MoneyRange(valueIn))
                {
                    this.Logger.LogTrace("(-)[BAD_TX_INPUT_VALUE]");
                    ConsensusErrors.BadTransactionInputValueOutOfRange.Throw();
                }
            }

            if (valueIn < transaction.TotalOut)
            {
                this.Logger.LogTrace("(-)[TX_IN_BELOW_OUT]");
                ConsensusErrors.BadTransactionInBelowOut.Throw();
            }

            // Tally transaction fees.
            Money txFee = valueIn - transaction.TotalOut;
            if (txFee < 0)
            {
                this.Logger.LogTrace("(-)[NEGATIVE_FEE]");
                ConsensusErrors.BadTransactionNegativeFee.Throw();
            }

            fees += txFee;
            if (!this.MoneyRange(fees))
            {
                this.Logger.LogTrace("(-)[BAD_FEE]");
                ConsensusErrors.BadTransactionFeeOutOfRange.Throw();
            }

            this.Logger.LogTrace("(-)");
        }

        /// <inheritdoc />
        public override long GetTransactionSignatureOperationCost(Transaction transaction, UnspentOutputSet inputs,
            DeploymentFlags flags)
        {
            long signatureOperationCost = this.GetLegacySignatureOperationsCount(transaction) *
                                          this.PowConsensusOptions.WitnessScaleFactor;

            if (transaction.IsCoinBase)
                return signatureOperationCost;

            if (flags.ScriptFlags.HasFlag(ScriptVerify.P2SH))
            {
                signatureOperationCost += this.GetP2SHSignatureOperationsCount(transaction, inputs) *
                                          this.PowConsensusOptions.WitnessScaleFactor;
            }

            signatureOperationCost += (from t in transaction.Inputs.RemoveChangePointer()
                let prevout = inputs.GetOutputFor(t)
                select this.CountWitnessSignatureOperation(prevout.ScriptPubKey, t.WitScript, flags)).Sum();

            return signatureOperationCost;
        }

        /// <inheritdoc />
        protected override uint GetP2SHSignatureOperationsCount(Transaction transaction, UnspentOutputSet inputs)
        {
            if (transaction.IsCoinBase)
                return 0;

            uint sigOps = 0;
            foreach (TxIn txIn in transaction.Inputs.RemoveChangePointer())
            {
                TxOut prevout = inputs.GetOutputFor(txIn);
                if (prevout.ScriptPubKey.IsPayToScriptHash(this.Parent.Network))
                    sigOps += prevout.ScriptPubKey.GetSigOpCount(this.Parent.Network, txIn.ScriptSig);
            }

            return sigOps;
        }

        ///<inheritdoc />
        protected override void UpdateUTXOSet(RuleContext context, Transaction transaction)
        {
            // Saves script pub keys and total amount of spent inputs to context
            
            this.Logger.LogTrace("()");

            ChainedHeader index = context.ValidationContext.ChainedHeader;

            UnspentOutputSet view = (context as UtxoRuleContext).UnspentOutputSet;
            switch (context)
            {
                case DeStreamPowRuleContext deStreamPowRuleContext:
                    deStreamPowRuleContext.InputScriptPubKeys.AddRange(transaction.Inputs.RemoveChangePointer()
                        .Select(p => view.GetOutputFor(p).ScriptPubKey));
                    deStreamPowRuleContext.TotalIn = view.GetValueIn(transaction);
                    break;
                case DeStreamRuleContext deStreamPosRuleContext:
                    deStreamPosRuleContext.InputScriptPubKeys.AddRange(transaction.Inputs.RemoveChangePointer()
                        .Select(p => view.GetOutputFor(p).ScriptPubKey));
                    deStreamPosRuleContext.TotalIn = view.GetValueIn(transaction);
                    break;
                default:
                    throw new NotSupportedException(
                        $"Rule context must be {nameof(DeStreamPowRuleContext)} or {nameof(DeStreamRuleContext)}");
            }

            view.Update(transaction, index.Height);

            this.Logger.LogTrace("(-)");
        }
    }
}
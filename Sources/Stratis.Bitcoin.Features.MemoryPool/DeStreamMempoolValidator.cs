using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Stratis.Bitcoin.Configuration;
using Stratis.Bitcoin.Consensus.Rules;
using Stratis.Bitcoin.Features.Consensus.CoinViews;
using Stratis.Bitcoin.Features.MemoryPool.Interfaces;
using Stratis.Bitcoin.Utilities;

namespace Stratis.Bitcoin.Features.MemoryPool
{
    /// <summary>
    ///     Creates <see cref="DeStreamMempoolCoinView" /> and prevents verifing ChangePointer input
    /// </summary>
    public class DeStreamMempoolValidator : MempoolValidator
    {
        public DeStreamMempoolValidator(ITxMempool memPool, MempoolSchedulerLock mempoolLock,
            IDateTimeProvider dateTimeProvider, MempoolSettings mempoolSettings, ConcurrentChain chain,
            CoinView coinView, ILoggerFactory loggerFactory, NodeSettings nodeSettings, IConsensusRules consensusRules)
            : base(memPool, mempoolLock, dateTimeProvider, mempoolSettings, chain, coinView, loggerFactory,
                nodeSettings, consensusRules)
        {
        }

        protected override async Task AcceptToMemoryPoolWorkerAsync(MempoolValidationState state, Transaction tx,
            List<uint256> vHashTxnToUncache)
        {
            var context = new MempoolValidationContext(tx, state);

            this.PreMempoolChecks(context);

            // create the MemPoolCoinView and load relevant utxoset
            context.View = new DeStreamMempoolCoinView(this.coinView, this.memPool, this.mempoolLock, this);
            await context.View.LoadViewAsync(context.Transaction).ConfigureAwait(false);

            // adding to the mem pool can only be done sequentially
            // use the sequential scheduler for that.
            await this.mempoolLock.WriteAsync(() =>
            {
                // is it already in the memory pool?
                if (this.memPool.Exists(context.TransactionHash))
                    state.Invalid(MempoolErrors.InPool).Throw();

                // Check for conflicts with in-memory transactions
                this.CheckConflicts(context);

                this.CheckMempoolCoinView(context);

                this.CreateMempoolEntry(context, state.AcceptTime);
                this.CheckSigOps(context);
                this.CheckFee(context);

                this.CheckRateLimit(context, state.LimitFree);

                this.CheckAncestors(context);
                this.CheckReplacment(context);
                this.CheckAllInputs(context);

                // Remove conflicting transactions from the mempool
                foreach (TxMempoolEntry it in context.AllConflicting)
                {
                    this.logger.LogInformation(
                        $"replacing tx {it.TransactionHash} with {context.TransactionHash} for {context.ModifiedFees - context.ConflictingFees} BTC additional fees, {context.EntrySize - context.ConflictingSize} delta bytes");
                }

                this.memPool.RemoveStaged(context.AllConflicting, false);

                // This transaction should only count for fee estimation if
                // the node is not behind and it is not dependent on any other
                // transactions in the mempool
                bool validForFeeEstimation = this.IsCurrentForFeeEstimation() && this.memPool.HasNoInputsOf(tx);

                // Store transaction in memory
                this.memPool.AddUnchecked(context.TransactionHash, context.Entry, context.SetAncestors,
                    validForFeeEstimation);

                // trim mempool and check if tx was trimmed
                if (!state.OverrideMempoolLimit)
                {
                    this.LimitMempoolSize(this.mempoolSettings.MaxMempool * 1000000,
                        this.mempoolSettings.MempoolExpiry * 60 * 60);

                    if (!this.memPool.Exists(context.TransactionHash))
                        state.Fail(MempoolErrors.Full).Throw();
                }

                // do this here inside the exclusive scheduler for better accuracy
                // and to avoid springing more concurrent tasks later
                state.MempoolSize = this.memPool.Size;
                state.MempoolDynamicSize = this.memPool.DynamicMemoryUsage();

                this.PerformanceCounter.SetMempoolSize(state.MempoolSize);
                this.PerformanceCounter.SetMempoolDynamicSize(state.MempoolDynamicSize);
                this.PerformanceCounter.AddHitCount(1);
            });
        }

        protected override void CheckMempoolCoinView(MempoolValidationContext context)
        {
            Guard.Assert(context.View != null);

            context.LockPoints = new LockPoints();

            // do we already have it?
            if (context.View.HaveCoins(context.TransactionHash))
                context.State.Invalid(MempoolErrors.AlreadyKnown).Throw();

            // do all inputs exist?
            // Note that this does not check for the presence of actual outputs (see the next check for that),
            // and only helps with filling in pfMissingInputs (to determine missing vs spent).
            foreach (TxIn txin in context.Transaction.Inputs.RemoveChangePointer())
            {
                if (context.View.HaveCoins(txin.PrevOut.Hash)) continue;
                context.State.MissingInputs = true;
                context.State.Fail(new MempoolError())
                    .Throw(); // fMissingInputs and !state.IsInvalid() is used to detect this condition, don't set state.Invalid()
            }

            // are the actual inputs available?
            if (!context.View.HaveInputs(context.Transaction))
                context.State.Invalid(MempoolErrors.BadInputsSpent).Throw();
        }

        /// <inheritdoc />
        protected override bool AreInputsStandard(Transaction tx, MempoolCoinView mapInputs)
        {
            if (tx.IsCoinBase)
                return true; // Coinbases don't use vin normally

            foreach (TxIn txin in tx.Inputs.RemoveChangePointer())
            {
                TxOut prev = mapInputs.GetOutputFor(txin);
                ScriptTemplate template = StandardScripts.GetTemplateFromScriptPubKey(prev.ScriptPubKey);
                if (template == null)
                    return false;

                if (template.Type != TxOutType.TX_SCRIPTHASH) continue;

                if (prev.ScriptPubKey.GetSigOpCount(true) > 15) //MAX_P2SH_SIGOPS
                    return false;
            }

            return true;
        }

        /// <inheritdoc />
        protected override void CheckConflicts(MempoolValidationContext context)
        {
            context.SetConflicts = new List<uint256>();
            foreach (TxIn txin in context.Transaction.Inputs.RemoveChangePointer())
            {
                TxMempool.NextTxPair itConflicting = this.memPool.MapNextTx.Find(f => f.OutPoint == txin.PrevOut);
                if (itConflicting == null) continue;
                
                Transaction ptxConflicting = itConflicting.Transaction;
                if (context.SetConflicts.Contains(ptxConflicting.GetHash())) continue;
                
                // Allow opt-out of transaction replacement by setting
                // nSequence >= maxint-1 on all inputs.
                //
                // maxint-1 is picked to still allow use of nLockTime by
                // non-replaceable transactions. All inputs rather than just one
                // is for the sake of multi-party protocols, where we don't
                // want a single party to be able to disable replacement.
                //
                // The opt-out ignores descendants as anyone relying on
                // first-seen mempool behavior should be checking all
                // unconfirmed ancestors anyway; doing otherwise is hopelessly
                // insecure.
                bool replacementOptOut = true;
                if (this.mempoolSettings.EnableReplacement)
                {
                    foreach (TxIn txiner in ptxConflicting.Inputs)
                    {
                        if (txiner.Sequence >= Sequence.Final - 1) continue;
                        
                        replacementOptOut = false;
                        break;
                    }
                }

                if (replacementOptOut)
                    context.State.Invalid(MempoolErrors.Conflict).Throw();

                context.SetConflicts.Add(ptxConflicting.GetHash());
            }
        }
        
        /// <summary>
        /// Validates the transaction fee is valid.
        /// </summary>
        /// <param name="context">Current validation context.</param>
        protected override void CheckFee(MempoolValidationContext context)
        {
            long expectedFee = Convert.ToInt64(context.Transaction.Outputs
                                                   .Where(p => !context.Transaction.Inputs.RemoveChangePointer()
                                                       .Select(q => context.View.Set.GetOutputFor(q).ScriptPubKey)
                                                       .Contains(p.ScriptPubKey))
                                                   .Sum(p => p.Value) * this.network.FeeRate);
            
            if (context.ModifiedFees < expectedFee)
                context.State.Fail(MempoolErrors.InsufficientFee, $" {context.Fees} < {expectedFee}").Throw();
        }
    }
}
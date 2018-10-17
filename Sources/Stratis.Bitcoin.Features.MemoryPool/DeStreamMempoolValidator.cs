using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Stratis.Bitcoin.Base.Deployments;
using Stratis.Bitcoin.Configuration;
using Stratis.Bitcoin.Consensus.Rules;
using Stratis.Bitcoin.Features.Consensus.CoinViews;
using Stratis.Bitcoin.Features.Consensus.Rules.CommonRules;
using Stratis.Bitcoin.Features.MemoryPool.Interfaces;
using Stratis.Bitcoin.Utilities;

namespace Stratis.Bitcoin.Features.MemoryPool
{
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
    }
}
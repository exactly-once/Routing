﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ExactlyOnce.Routing.Controller.Model.Azure
{
    public class ExactlyOnceProcessor
    {
        readonly IOutboxStore outboxStore;
        readonly IStateStore stateStore;

        public ExactlyOnceProcessor(IOutboxStore outboxStore, IStateStore stateStore)
        {
            this.outboxStore = outboxStore;
            this.stateStore = stateStore;
        }

        public async Task<TSideEffect> Process<TSideEffect>(string requestId, string stateId, Type stateType,
            Func<object, TSideEffect> handle, CancellationToken cancellationToken = default)
        {
            var (state, version) = await stateStore.Load(stateId, stateType, cancellationToken)
                .ConfigureAwait(false);

            if (state.TxId != null)
            {
                await FinishTransaction(stateId, state, version, cancellationToken)
                    .ConfigureAwait(false);
            }

            var outboxState = await outboxStore.Get(stateId, requestId, cancellationToken);

            if (outboxState != null)
            {
                return JsonConvert.DeserializeObject<TSideEffect>(outboxState.SideEffect);
            }

            var sideEffect = handle(state);

            state.TxId = Guid.NewGuid();

            outboxState = new OutboxItem
            {
                Id = state.TxId.ToString(),
                StateId = stateId,
                RequestId = requestId,
                SideEffect = JsonConvert.SerializeObject(sideEffect)
            };

            await outboxStore.Store(outboxState, cancellationToken)
                .ConfigureAwait(false);

            object nextVersion;

            try
            {
                nextVersion = await stateStore.Upsert(stateId, state, version, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception e) when (!(e is OperationCanceledException))
            {
                await outboxStore.Delete(stateId, outboxState.Id, cancellationToken)
                    .ConfigureAwait(false);
                throw;
            }

            await FinishTransaction(stateId, state, nextVersion, cancellationToken)
                .ConfigureAwait(false);

            return sideEffect;
        }

        async Task FinishTransaction<TState>(string stateId, TState state, object version, CancellationToken cancellationToken = default) where TState : State
        {
            if (state.TxId.HasValue == false)
            {
                throw new InvalidOperationException($"No pending transaction for state id {stateId}.");
            }

            await outboxStore.Commit(stateId, state.TxId.Value.ToString(), cancellationToken)
                .ConfigureAwait(false);

            state.TxId = null;
            await stateStore.Upsert(stateId, state, version, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
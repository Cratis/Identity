// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.Erasing;

/// <summary>
/// Destroys the subject's encryption key through the Chronicle compliance API, once per tenant or realm namespace
/// (Chronicle has no cross-namespace erasure). It never authorizes a replacement key.
/// </summary>
/// <param name="client">The Chronicle client used to reach each namespace.</param>
/// <param name="eventStore">The account event store whose name addresses the other namespaces.</param>
internal class DestroySubjectKeysStage(IChronicleClient client, IEventStore eventStore) : IErasureStage
{
    static readonly ReasonId _failed = "erasure_key_destruction_failed";

    /// <inheritdoc/>
    public ErasureStageId Id => ErasureStages.DestroySubjectKeys;

    /// <inheritdoc/>
    public async Task<ErasureStageOutcome> Execute(ErasureContext context)
    {
        var covered = new List<string>();
        ReasonId? failure = null;
        foreach (var @namespace in context.Namespaces)
        {
            try
            {
                var store = await client.GetEventStore(eventStore.Name, @namespace);
                await store.PII.DeleteEncryptionKeyFor(context.Account.Value);
                covered.Add(@namespace);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                failure = _failed;
            }
        }

        // Attempt every namespace so one failure does not leave the others untouched; the retry is safe (the fence is recorded first).
        return failure is null ? ErasureStageOutcome.Success(covered) : ErasureStageOutcome.Failure(failure);
    }
}

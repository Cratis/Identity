// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.Erasing;

/// <summary>The allowlisted erasure stage names and their mandatory order (identity-events-v1 §5).</summary>
internal static class ErasureStages
{
    /// <summary>Revokes grants, families, sessions and PATs first, through the revocation protocol.</summary>
    internal static readonly ErasureStageId Revoke = "revoke";

    /// <summary>Destroys every subject key revision in every relevant tenant or realm namespace.</summary>
    internal static readonly ErasureStageId DestroySubjectKeys = "destroy-subject-keys";

    /// <summary>Removes projections and caches that hold the subject.</summary>
    internal static readonly ErasureStageId DeleteReadModels = "delete-read-models";

    /// <summary>Purges personal data from the operational store.</summary>
    internal static readonly ErasureStageId PurgeOperationalStore = "purge-operational-store";

    /// <summary>Collects consumer cleanup acknowledgements.</summary>
    internal static readonly ErasureStageId ConsumerAcknowledgements = "consumer-acknowledgements";

    /// <summary>Gets every stage that must be present, in execution order.</summary>
    internal static IReadOnlyList<ErasureStageId> Required { get; } = [Revoke, DestroySubjectKeys, DeleteReadModels, PurgeOperationalStore, ConsumerAcknowledgements];
}

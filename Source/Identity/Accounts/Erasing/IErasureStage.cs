// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.Erasing;

/// <summary>
/// One ordered step of the erasure workflow. Implementations MUST be idempotent: the coordinator re-runs every
/// stage on retry. A stage MUST NOT call AllowNewEncryptionKeyFor.
/// </summary>
internal interface IErasureStage
{
    /// <summary>Gets the stage name; it must be one of <see cref="ErasureStages.Required"/>.</summary>
    ErasureStageId Id { get; }

    /// <summary>Runs the stage for one subject.</summary>
    /// <param name="context">The subject and namespaces.</param>
    /// <returns>Success, or a failure carrying only an allowlisted reason.</returns>
    Task<ErasureStageOutcome> Execute(ErasureContext context);
}

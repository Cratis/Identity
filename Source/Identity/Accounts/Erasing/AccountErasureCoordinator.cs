// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;

namespace Cratis.Identity.Accounts.Erasing;

/// <summary>
/// Runs the ordered erasure stages and records completion only when every stage succeeded. It refuses to run
/// (appending nothing) unless the boundary is approved, a namespace scope exists and every required stage is registered.
/// </summary>
/// <param name="log">The account stream.</param>
/// <param name="stages">The registered stages.</param>
/// <param name="scope">The namespace scope.</param>
/// <param name="boundary">The approved completion boundary.</param>
/// <param name="time">The trusted clock.</param>
internal class AccountErasureCoordinator(IAccountErasureLog log, IEnumerable<IErasureStage> stages, IErasureTenantScope scope, IErasureBoundaryPolicy boundary, TimeProvider time)
{
    static readonly ReasonId _stageException = "erasure_stage_exception";

    /// <summary>Runs, or safely resumes, the erasure; every stage is re-run so stages must be idempotent.</summary>
    /// <param name="account">The account subject.</param>
    /// <param name="erasureId">The accepted request.</param>
    /// <returns>The run result.</returns>
    /// <exception cref="AccountOperationRefused">The request or its configuration does not allow a run.</exception>
    public async Task<ErasureRunResult> Run(AccountSubject account, ErasureId erasureId)
    {
        var state = await log.Read(account);
        AccountOperationRefused.Require(state.Requested == erasureId, "erasure_not_requested");
        if (state.Completed)
        {
            return new(true, null);
        }

        var approved = boundary.Approved;
        AccountOperationRefused.Require(approved is not null, "erasure_boundary_unconfigured");
        var ordered = stages.ToArray();
        AccountOperationRefused.Require(ordered.Select(stage => stage.Id).SequenceEqual(ErasureStages.Required), "erasure_stages_incomplete");
        var namespaces = await scope.NamespacesFor(account);
        AccountOperationRefused.Require(namespaces.Count > 0, "erasure_tenant_scope_unconfigured");

        var context = new ErasureContext(account, erasureId, namespaces);
        foreach (var stage in ordered)
        {
            var outcome = await Execute(stage, context);
            if (!outcome.Succeeded)
            {
                await RecordFailure(account, state, erasureId, stage.Id, outcome.Reason!);
                return new(false, stage.Id);
            }
        }

        await log.Append(account, new AccountErasureRecorded(erasureId, time.GetUtcNow(), approved!, new FactId(erasureId.Value)));
        return new(true, null);
    }

    static async Task<ErasureStageOutcome> Execute(IErasureStage stage, ErasureContext context)
    {
        try
        {
            return await stage.Execute(context);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return ErasureStageOutcome.Failure(_stageException);
        }
    }

    async Task RecordFailure(AccountSubject account, ErasureState state, ErasureId erasureId, ErasureStageId stage, ReasonId reason)
    {
        // A repeat of the already recorded failure is not a new transition; do not append retry noise.
        if (state.LastFailure == (stage, reason))
        {
            return;
        }

        await log.Append(account, new AccountErasureFailed(erasureId, stage, reason));
    }
}

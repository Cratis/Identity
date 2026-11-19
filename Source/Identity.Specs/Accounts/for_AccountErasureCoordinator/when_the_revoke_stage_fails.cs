// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Accounts.Erasing;
using Cratis.Identity.Contracts.Accounts.Events;

namespace Cratis.Identity.Accounts.for_AccountErasureCoordinator;

public class when_the_revoke_stage_fails : given.an_erasure_workflow
{
    ErasureRunResult _result;

    async Task Establish()
    {
        await Tombstone();
        await Requests.Request(new ClaimsPrincipal(), _account, _erasure);
        _stages[0].Outcome = ErasureStageOutcome.Failure("revocation_incomplete");
    }

    async Task Because() => _result = await Coordinator.Run(_account, _erasure);

    [Fact] void should_name_revoke_as_the_failed_stage() => _result.FailedStage.ShouldEqual(ErasureStages.Revoke);
    [Fact] void should_run_no_stage_after_revoke() => _executed.ShouldEqual(["revoke"]);
    [Fact] void should_not_complete() => _log.Facts.OfType<AccountErasureRecorded>().ShouldBeEmpty();
}

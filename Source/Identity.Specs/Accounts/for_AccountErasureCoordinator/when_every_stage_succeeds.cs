// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Accounts.Erasing;
using Cratis.Identity.Contracts.Accounts.Events;

namespace Cratis.Identity.Accounts.for_AccountErasureCoordinator;

public class when_every_stage_succeeds : given.an_erasure_workflow
{
    ErasureRunResult _result;
    ErasureRunResult _retry;

    async Task Establish()
    {
        await Tombstone();
        await Requests.Request(new ClaimsPrincipal(), _account, _erasure);
    }

    async Task Because()
    {
        _result = await Coordinator.Run(_account, _erasure);
        _retry = await Coordinator.Run(_account, _erasure);
    }

    [Fact] void should_complete() => _result.Completed.ShouldBeTrue();
    [Fact] void should_run_stages_in_the_mandated_order() => _executed.ShouldEqual(ErasureStages.Required.Select(stage => stage.Value));
    [Fact] void should_record_completion_once_with_the_boundary() => _log.Facts.OfType<AccountErasureRecorded>().Single().BoundaryVersion.Value.ShouldEqual(1);
    [Fact] void should_not_rerun_or_rerecord_on_retry() => (_retry.Completed && _executed.Count == 5 && _log.Facts.OfType<AccountErasureRecorded>().Count() == 1).ShouldBeTrue();
    [Fact] void should_append_no_failure() => _log.Facts.OfType<AccountErasureFailed>().ShouldBeEmpty();
}

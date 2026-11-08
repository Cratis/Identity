// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Accounts.Erasing;
using Cratis.Identity.Contracts.Accounts.Events;

namespace Cratis.Identity.Accounts.for_AccountErasureCoordinator;

public class when_a_stage_fails : given.an_erasure_workflow
{
    ErasureRunResult _first;
    ErasureRunResult _repeat;
    ErasureRunResult _healed;

    async Task Establish()
    {
        await Tombstone();
        await Requests.Request(new ClaimsPrincipal(), _account, _erasure);
        _stages[1].Outcome = ErasureStageOutcome.Failure("key_erasure_incomplete");
    }

    async Task Because()
    {
        _first = await Coordinator.Run(_account, _erasure);
        _repeat = await Coordinator.Run(_account, _erasure);
        _stages[1].Outcome = null;
        _healed = await Coordinator.Run(_account, _erasure);
    }

    [Fact] void should_not_complete_while_failing() => (_first.Completed || _repeat.Completed).ShouldBeFalse();
    [Fact] void should_name_the_failed_stage() => _first.FailedStage.ShouldEqual(ErasureStages.DestroySubjectKeys);
    [Fact] void should_append_one_failure_for_repeated_identical_failures() => _log.Facts.OfType<AccountErasureFailed>().Count().ShouldEqual(1);
    [Fact] void should_carry_only_the_stage_and_reason() => _log.Facts.OfType<AccountErasureFailed>().Single().ReasonId.Value.ShouldEqual("key_erasure_incomplete");
    [Fact] void should_not_run_later_stages_while_failing() => _executed.Take(4).ShouldEqual(["revoke", "destroy-subject-keys", "revoke", "destroy-subject-keys"]);
    [Fact] void should_complete_after_the_stage_recovers() => (_healed.Completed && _log.Facts.OfType<AccountErasureRecorded>().Count() == 1).ShouldBeTrue();
}

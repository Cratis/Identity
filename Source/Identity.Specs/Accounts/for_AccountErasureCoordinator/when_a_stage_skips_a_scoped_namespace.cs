// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Accounts.Erasing;
using Cratis.Identity.Contracts.Accounts.Events;

namespace Cratis.Identity.Accounts.for_AccountErasureCoordinator;

public class when_a_stage_skips_a_scoped_namespace : given.an_erasure_workflow
{
    ErasureRunResult _result;

    async Task Establish()
    {
        await Tombstone();
        await Requests.Request(new ClaimsPrincipal(), _account, _erasure);
        _stages[1].Covered = ["tenant-a"];
    }

    async Task Because() => _result = await Coordinator.Run(_account, _erasure);

    [Fact] void should_fail_that_stage() => _result.FailedStage.ShouldEqual(ErasureStages.DestroySubjectKeys);
    [Fact] void should_record_the_allowlisted_reason() => _log.Facts.OfType<AccountErasureFailed>().Single().ReasonId.Value.ShouldEqual("erasure_namespace_not_covered");
    [Fact] void should_not_complete() => _log.Facts.OfType<AccountErasureRecorded>().ShouldBeEmpty();
}

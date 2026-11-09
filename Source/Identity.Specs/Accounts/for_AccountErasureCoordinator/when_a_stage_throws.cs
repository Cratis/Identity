// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Accounts.Erasing;
using Cratis.Identity.Contracts.Accounts.Events;

namespace Cratis.Identity.Accounts.for_AccountErasureCoordinator;

public class when_a_stage_throws : given.an_erasure_workflow
{
    ErasureRunResult _result;

    async Task Establish()
    {
        await Tombstone();
        await Requests.Request(new ClaimsPrincipal(), _account, _erasure);
        _stages[0].Throws = new InvalidOperationException("contains personal@example.test");
    }

    async Task Because() => _result = await Coordinator.Run(_account, _erasure);

    [Fact] void should_fail_the_stage() => _result.FailedStage.ShouldEqual(ErasureStages.Revoke);
    [Fact] void should_not_leak_the_exception_message() => _log.Facts.OfType<AccountErasureFailed>().Single().ReasonId.Value.ShouldEqual("erasure_stage_exception");
    [Fact] void should_not_complete() => _log.Facts.OfType<AccountErasureRecorded>().ShouldBeEmpty();
}

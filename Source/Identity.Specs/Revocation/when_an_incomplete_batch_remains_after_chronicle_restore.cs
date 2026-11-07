// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;

namespace Cratis.Identity.RevocationSpecs;

public class when_an_incomplete_batch_remains_after_chronicle_restore : given.a_recovery_pass
{
    void Establish()
    {
        _options.RestoreKind = RestoreKind.Chronicle;
        _options.RestoreId = "batched-restore-test";
        _options.RestrictionsReestablished = true;
        _fences.Undelivered(1, Arg.Any<CancellationToken>()).Returns(_ => [new GrantState("pending", 1, null, _record.Fence, _record.Intents)]);
    }

    async Task Because() => await Run();

    [Fact] void should_not_treat_one_batch_as_complete_recovery() => _gate.Ready.ShouldBeFalse();
    [Fact] void should_not_publish_a_complete_prefix() => _facts.DidNotReceive().Checkpoint(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<CancellationToken>());
    [Fact] void should_require_another_bounded_pass() => _failure.ShouldBeOfExactType<RevocationPending>();
}

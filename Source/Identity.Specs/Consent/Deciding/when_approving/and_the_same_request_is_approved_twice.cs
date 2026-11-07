// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.ConsentFlow.Deciding.when_approving;

public class and_the_same_request_is_approved_twice : given.an_activation
{
    string?[] _codes;

    async Task Because()
    {
        await Task.WhenAll(Approve(), Approve());
        var arrived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var writers = 0;
        evidence.BeforeRecord = async () =>
        {
            if (Interlocked.Increment(ref writers) == 2) arrived.SetResult();
            await arrived.Task;
        };
        await Task.WhenAll(Resume(), Restart().Resume(request.Id, CancellationToken.None));
        _codes = await Task.WhenAll(Claim(), Restart().ClaimCode(request.Id, binding, CancellationToken.None));
    }

    [Fact] void should_append_one_evidence_pair() => evidence.Appends.ShouldEqual(1);
    [Fact] void should_prepare_one_grant() => authorizations.Count.ShouldEqual(1);
    [Fact] void should_claim_code_issuance_once() => _codes.Count(_ => _ is not null).ShouldEqual(1);
}

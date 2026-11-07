// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.ConsentFlow.Deciding.when_approving;

public class and_the_same_request_is_approved_twice : given.an_activation
{
    string?[] _codes;

    async Task Because()
    {
        store.BeforeCompareExchange = AtTheSameRevision();
        await Task.WhenAll(Approve(), Approve());
        store.BeforeCompareExchange = null;
        var arrived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var writers = 0;
        evidence.BeforeRecord = async () =>
        {
            if (Interlocked.Increment(ref writers) == 2) arrived.SetResult();
            await arrived.Task;
        };
        await Task.WhenAll(Resume(), Restart().Resume(request.Id, CancellationToken.None));
        store.BeforeCompareExchange = AtTheSameRevision();
        _codes = await Task.WhenAll(Claim(), Restart().ClaimCode(request.Id, binding, CancellationToken.None));
    }

    static Func<ConsentRequest, ConsentRequest, Task> AtTheSameRevision()
    {
        var arrived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var writers = 0;
        return async (_, _) =>
        {
            if (Interlocked.Increment(ref writers) == 2) arrived.SetResult();
            await arrived.Task;
        };
    }

    [Fact] void should_have_both_approvals_observe_the_same_revision() => store.Attempts.Where(_ => _.Replacement.State == ConsentRequestState.Decided).Select(_ => _.Expected.Revision).Distinct().Count().ShouldEqual(1);
    [Fact] void should_have_one_approval_lose_the_compare_exchange() => store.Attempts.Count(_ => _.Replacement.State == ConsentRequestState.Decided && !_.Won).ShouldEqual(1);
    [Fact] void should_append_one_evidence_pair() => evidence.Appends.ShouldEqual(1);
    [Fact] void should_prepare_one_grant() => authorizations.Count.ShouldEqual(1);
    [Fact] void should_have_both_code_claims_observe_the_same_revision() => store.Attempts.Where(_ => _.Replacement.CodeClaimed).Select(_ => _.Expected.Revision).Distinct().Count().ShouldEqual(1);
    [Fact] void should_have_one_code_claim_lose_the_compare_exchange() => store.Attempts.Count(_ => _.Replacement.CodeClaimed && !_.Won).ShouldEqual(1);
    [Fact] void should_claim_code_issuance_once() => _codes.Count(_ => _ is not null).ShouldEqual(1);
    [Fact] void should_deny_issuance_to_the_losing_compare_exchange() => _codes.Count(_ => _ is null).ShouldEqual(1);
}

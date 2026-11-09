// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.ConsentFlow.Deciding;

public class when_reconciling_after_a_request_fails : given.an_activation
{
    ConsentRequest[] _firstPass;
    ConsentRequest[] _secondPass;

    async Task Because()
    {
        var requests = new[] { request, await activation.Create(binding, CancellationToken.None), await activation.Create(binding, CancellationToken.None) };
        foreach (var row in requests) await activation.Decide(row.Id, binding, true, CancellationToken.None);
        var services = new ServiceCollection();
        services.AddSingleton<IConsentRequestStore>(store);
        services.AddSingleton(activation);
        await using var provider = services.BuildServiceProvider();
        using var reconciler = new ConsentReconciler(provider.GetRequiredService<IServiceScopeFactory>(), Options.Create(options), clock, NullLogger<ConsentReconciler>.Instance);
        store.StopBefore = ConsentRequestState.EvidenceRecorded;
        await reconciler.Reconcile(CancellationToken.None);
        _firstPass = (await Task.WhenAll(requests.Select(_ => store.Read(_.Id, CancellationToken.None))))
            .Select(_ => _ ?? throw new InvalidOperationException("Request missing after reconciliation")).ToArray();
        await reconciler.Reconcile(CancellationToken.None); // Reach the end of the cursor scan.
        await reconciler.Reconcile(CancellationToken.None); // Retry from the beginning.
        _secondPass = (await Task.WhenAll(requests.Select(_ => store.Read(_.Id, CancellationToken.None))))
            .Select(_ => _ ?? throw new InvalidOperationException("Request missing after reconciliation")).ToArray();
    }

    [Fact] void should_not_let_one_failure_block_the_batch() => _firstPass.Count(_ => _.State == ConsentRequestState.Active).ShouldEqual(2);
    [Fact] void should_retry_the_failed_request_on_a_later_scan() => _secondPass.All(_ => _.State == ConsentRequestState.Active).ShouldBeTrue();
    [Fact] void should_never_issue_codes_in_the_background() => _secondPass.All(_ => !_.CodeClaimed).ShouldBeTrue();
    [Fact] void should_not_duplicate_evidence_after_the_failure() => evidence.Appends.ShouldEqual(3);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Identity.ConsentFlow.Deciding;

public class when_registering_fail_closed_defaults : given.an_activation
{
    ConsentRequest _after;
    string? _code;
    bool _fence;
    string? _prepared;
    ConsentEvidenceResolution _resolution;

    async Task Because()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(gate);
        services.AddSingleton<IConsentRequestStore>(store);
        services.AddSingleton<TimeProvider>(clock);
        services.AddConsentActivation(new ConfigurationBuilder().Build());
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        activation = scope.ServiceProvider.GetRequiredService<ConsentActivation>();
        await Approve();
        _after = await Resume();
        _code = await Claim();
        _fence = await scope.ServiceProvider.GetRequiredService<IRevocationFence>().IsClear(request, CancellationToken.None);
        _prepared = await scope.ServiceProvider.GetRequiredService<IAuthorizationActivator>().Prepare(request, CancellationToken.None);
        _resolution = await scope.ServiceProvider.GetRequiredService<IConsentEvidence>().Resolve(request.Id, CancellationToken.None);
    }

    [Fact] void should_never_advance_without_a_real_writer() => _after.State.ShouldEqual(ConsentRequestState.Decided);
    [Fact] void should_deny_a_code() => _code.ShouldBeNull();
    [Fact] void should_deny_the_default_fence_check() => _fence.ShouldBeFalse();
    [Fact] void should_not_prepare_an_authorization() => _prepared.ShouldBeNull();
    [Fact] void should_not_infer_absence_from_an_unconfigured_writer() => _resolution.ShouldEqual(ConsentEvidenceResolution.Unknown);
}

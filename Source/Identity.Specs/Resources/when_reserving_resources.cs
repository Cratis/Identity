// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.EventSequences;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Resources;
using Cratis.Identity.Contracts.Resources.Events;
using Cratis.Identity.Resources.Registering;

namespace Cratis.Identity.Resources;

public class when_reserving_resources : given.resource_commands
{
    [Theory]
    [InlineData("https://api.example", "other", "other")]
    [InlineData("https://other.example", "api", "other")]
    [InlineData("https://other.example", "other", "api")]
    public async Task should_permanently_reserve_each_catalogue_identity(string indicator, string audience, string prefix)
    {
        using var scenario = new EventScenario();
        await scenario.Given.ForEventSource(_id.Value).Events(Registered());
        await scenario.Given.ForEventSource(_id.Value).Events(new ResourceScopeRetirementRecorded("api:read", Now, FactId.New()));
        var duplicate = await scenario.When.ForEventSource(ResourceId.New().Value).Events(Registered(indicator, audience, prefix));
        duplicate.Errors.ShouldBeEmpty();
        duplicate.HasConstraintViolations.ShouldBeTrue();
        var distinct = await scenario.When.ForEventSource(ResourceId.New().Value).Events(Registered("https://other.example", "other", "other"));
        distinct.Errors.ShouldBeEmpty();
        distinct.IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData("https://API.EXAMPLE:443/", "https://api.example")]
    [InlineData("https://bücher.example", "https://xn--bcher-kva.example")]
    [InlineData("https://xn--bcher-kva.example", "https://bücher.example")]
    [InlineData("https://BÜCHER.EXAMPLE.:443/", "https://xn--bcher-kva.example")]
    [InlineData("https://api.example.", "https://API.EXAMPLE:443/")]
    public async Task should_reserve_normalized_indicator_aliases_before_append(string original, string alias)
    {
        using var scenario = new EventScenario();
        var first = new RegisterResource(original, "api", "api", new("API")).Handle(_decision).AsT0.Single();
        var second = new RegisterResource(alias, "other", "other", new("Other")).Handle(_decision).AsT0.Single();
        ((ResourceRegistrationRecorded)first.Event).ResourceIndicator.ShouldEqual(((ResourceRegistrationRecorded)second.Event).ResourceIndicator);
        await scenario.Given.ForEventSource(first.EventSourceId).Events(first.Event);
        var duplicate = await scenario.When.ForEventSource(second.EventSourceId).Events(second.Event);
        duplicate.Errors.ShouldBeEmpty();
        duplicate.HasConstraintViolations.ShouldBeTrue();
    }

    [Fact]
    async Task should_allow_only_one_concurrent_resource_registration()
    {
        using var scenario = new EventScenario();
        var first = new RegisterResource("https://api.example", "api", "api", new("API")).Handle(_decision).AsT0.Single();
        var second = new RegisterResource("https://API.EXAMPLE:443/", "api", "api", new("API")).Handle(_decision).AsT0.Single();
        var results = await Task.WhenAll(
            scenario.When.ForEventSource(first.EventSourceId).Events(first.Event),
            scenario.When.ForEventSource(second.EventSourceId).Events(second.Event));
        results.SelectMany(result => result.Errors).ShouldBeEmpty();
        results.Count(result => result.IsSuccess).ShouldEqual(1);
        results.Count(result => result.HasConstraintViolations).ShouldEqual(1);
    }

    [Fact]
    async Task should_never_release_a_retired_scope_reservation()
    {
        using var scenario = new EventScenario();
        await scenario.Given.ForEventSource(_id.Value).Events(Registered(), new ResourceScopeRegistrationRecorded("api:read", new("Read"), FactId.New()));
        await scenario.Given.ForEventSource(_id.Value).Events(new ResourceScopeRegistrationRecorded("api:write", new("Write"), FactId.New()));
        await scenario.Given.ForEventSource(_id.Value).Events(new ResourceScopeRetirementRecorded("api:read", Now, FactId.New()));
        var differentStream = await scenario.When.ForEventSource(ResourceId.New().Value).Events(new ResourceScopeRegistrationRecorded("api:read", new("Replacement"), FactId.New()));
        differentStream.Errors.ShouldBeEmpty();
        differentStream.HasConstraintViolations.ShouldBeTrue();
    }
}

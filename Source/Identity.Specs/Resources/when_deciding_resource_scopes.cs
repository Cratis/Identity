// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Cratis.Identity.Contracts.Resources;
using Cratis.Identity.Contracts.Resources.Events;
using Cratis.Identity.Resources.Registering;
using Cratis.Identity.Resources.RegisteringScope;
using Cratis.Identity.Resources.RetiringScope;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.Resources;

[PerFactSpecificationLifecycle]
public class when_deciding_resource_scopes : given.resource_commands
{
    [Fact]
    void should_require_a_registered_resource()
    {
        new RegisterResourceScope(_id, "api:read", new("Read")).Handle(null, _catalog).IsT1.ShouldBeTrue();
        new RetireResourceScope(_id, "api:read").Handle(null, _clock).IsT1.ShouldBeTrue();
        new RegisterResourceScope(_id, "api:read", new("Read")).Handle(new(_id), _catalog).IsT1.ShouldBeTrue();
    }

    [Fact]
    void should_refuse_empty_null_or_mismatched_stream_ids()
    {
        foreach (var id in new ResourceId[] { new(Guid.Empty), null!, ResourceId.New() })
        {
            new RegisterResourceScope(id, "api:read", new("Read")).Handle(_registered, _catalog).IsT1.ShouldBeTrue();
            new RetireResourceScope(id, "api:read").Handle(WithScope(), _clock).IsT1.ShouldBeTrue();
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("read")]
    [InlineData("other:read")]
    [InlineData("api:admin")]
    [InlineData("api:approve")]
    [InlineData("api:Read")]
    [InlineData("api:read:all")]
    [InlineData("api:read write")]
    [InlineData("api:read--all")]
    [InlineData("api:read-")]
    [InlineData("api:read\n")]
    public void should_refuse_invalid_and_foreign_scope_names(string? name)
    {
        new RegisterResourceScope(_id, name, new("Read")).Handle(_registered, _catalog).IsT1.ShouldBeTrue();
        new RetireResourceScope(_id, name).Handle(WithScope(), _clock).IsT1.ShouldBeTrue();
    }

    [Fact]
    void should_bound_scope_names_and_business_descriptions()
    {
        new RegisterResourceScope(_id, $"api:{new string('a', 65)}", new("Read")).Handle(_registered, _catalog).IsT1.ShouldBeTrue();
        foreach (var description in new[] { "", " ", "Read\rAPI", new string('a', 257) })
        {
            new RegisterResourceScope(_id, "api:read", new(description)).Handle(_registered, _catalog).IsT1.ShouldBeTrue();
        }
        new RegisterResourceScope(_id, "api:read", null!).Handle(_registered, _catalog).IsT1.ShouldBeTrue();
    }

    [Fact]
    void should_never_silently_reregister_a_scope()
    {
        var state = WithScope();
        new RegisterResourceScope(_id, "api:read", new("Replacement")).Handle(state, _catalog).IsT1.ShouldBeTrue();
        var retirement = new RetireResourceScope(_id, "api:read").Handle(state, _clock).AsT0.Single();
        var retired = _reducer.On((ResourceScopeRetirementRecorded)retirement.Event, state, _context);
        new RegisterResourceScope(_id, "api:read", new("Replacement")).Handle(retired, _catalog).IsT1.ShouldBeTrue();
        new RetireResourceScope(_id, "api:read").Handle(retired, _clock).IsT1.ShouldBeTrue();
        new RetireResourceScope(_id, "api:missing").Handle(state, _clock).IsT1.ShouldBeTrue();
    }

    [Fact]
    void should_emit_only_internal_scope_facts_on_the_selected_resource()
    {
        var registered = new RegisterResourceScope(_id, "api:read", new("Read API")).Handle(_registered, _catalog).AsT0.Single();
        registered.EventSourceId.ShouldEqual(_context.EventSourceId);
        registered.Event.ShouldBeOfExactType<ResourceScopeRegistrationRecorded>();
        var fact = (ResourceScopeRegistrationRecorded)registered.Event;
        fact.ScopeName.ShouldEqual("api:read");
        fact.Description.Value.ShouldEqual("Read API");
        fact.FactId.Value.ShouldNotEqual(Guid.Empty);
        var retirement = new RetireResourceScope(_id, "api:read").Handle(WithScope(), _clock).AsT0.Single();
        retirement.EventSourceId.ShouldEqual(_context.EventSourceId);
        retirement.Event.ShouldBeOfExactType<ResourceScopeRetirementRecorded>();
        var retired = (ResourceScopeRetirementRecorded)retirement.Event;
        retired.RetiredAt.ShouldEqual(Now);
        retired.FactId.Value.ShouldNotEqual(Guid.Empty);
    }

    [Fact]
    void should_refuse_configuration_collisions_on_later_scope_registration()
    {
        var catalog = new ResourceCatalog(Options.Create(new IdentityOptions
        {
            Resources = [new() { Audience = "Configured", Indicators = ["https://api.example/"], Scopes = [new() { Name = "api:read" }] }]
        }));
        new RegisterResourceScope(_id, "api:write", new("Write")).Handle(_registered, catalog).IsT1.ShouldBeTrue();
    }

    [Theory]
    [InlineData("https://other.example", "Configured", "other", true)]
    [InlineData("https://API.EXAMPLE:443/", "other", "other", true)]
    [InlineData("https://other.example", "other", "api", true)]
    [InlineData("https://other.example", "other", "other", false)]
    void should_detect_configuration_conflicts_by_audience_indicator_alias_and_scope_namespace(string indicator, string audience, string prefix, bool conflicts)
    {
        ResourceServer[] configured = [new("Configured", "Configured", ["https://api.example/"], [new("api:read", "Read")])];
        RegisterResource.Conflicts(configured, ResourceRegistrationValidation.NormalizeIndicator(indicator), audience, prefix).ShouldEqual(conflicts);
    }

    [Fact]
    void should_refuse_invalid_server_retirement_instants()
    {
        foreach (var at in new[] { DateTimeOffset.MinValue, DateTimeOffset.UnixEpoch.AddTicks(-1), DateTimeOffset.MaxValue })
        {
            _clock.Now = at;
            new RetireResourceScope(_id, "api:read").Handle(WithScope(), _clock).IsT1.ShouldBeTrue();
        }
    }
}

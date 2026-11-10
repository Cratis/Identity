// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Resources.Events;
using Cratis.Identity.Resources.RegisteringScope;
using Cratis.Identity.Resources.RetiringScope;

namespace Cratis.Identity.Resources;

public class when_rebuilding_resource_registration : given.resource_commands
{
    [Fact]
    void should_admit_the_internal_reducer_for_guarded_decisions() =>
        DecisionReadAdmissions.ShouldAdmitDecisionReadsIn(typeof(ResourceRegistration).Assembly);

    [Theory]
    [InlineData(typeof(RegisterResourceScope))]
    [InlineData(typeof(RetireResourceScope))]
    void should_require_guarded_resource_lookup(Type command)
    {
        command.GetCustomAttributes(typeof(ReadProtectionAttribute), false).Length.ShouldEqual(1);
        var state = command.GetMethod("Handle").GetParameters().Single(parameter => parameter.ParameterType == typeof(ResourceRegistration));
        state.GetCustomAttributes(typeof(LookUpByAttribute), false).Length.ShouldEqual(1);
    }

    [Fact]
    void should_preserve_all_scopes_labels_and_retirement_tombstones()
    {
        _registered.Id.ShouldEqual(_id);
        _registered.DisplayName.Value.ShouldEqual("API");
        _registered.ResourceIndicator.ShouldEqual("https://api.example");
        var first = WithScope();
        var second = _reducer.On(new ResourceScopeRegistrationRecorded("api:write", new("Write API"), FactId.New()), first, _context);
        var retired = _reducer.On(new ResourceScopeRetirementRecorded("api:read", Now, FactId.New()), second, _context);
        retired.Scopes.Count.ShouldEqual(2);
        retired.Scopes.Single(scope => scope.ScopeName == "api:read").Description.Value.ShouldEqual("Read API");
        retired.Scopes.Single(scope => scope.ScopeName == "api:read").RetiredAt.ShouldEqual(Now);
        retired.Scopes.Single(scope => scope.ScopeName == "api:write").Retired.ShouldBeFalse();
        var repeated = _reducer.On(new ResourceScopeRegistrationRecorded("api:read", new("Replacement"), FactId.New()), retired, _context);
        repeated.ShouldEqual(retired);
        _reducer.On(new ResourceScopeRetirementRecorded("api:read", Now.AddDays(1), FactId.New()), repeated, _context).Scopes.ToArray().ShouldEqual(retired.Scopes.ToArray());
    }

    [Fact]
    void should_never_resurrect_an_orphan_retirement_on_replay()
    {
        var retired = _reducer.On(new ResourceScopeRetirementRecorded("api:read", Now, FactId.New()), null, _context);
        var registered = _reducer.On(Registered(), retired, _context);
        var repeated = _reducer.On(new ResourceScopeRegistrationRecorded("api:read", new("Replacement"), FactId.New()), registered, _context);
        repeated.Scopes.Single().Retired.ShouldBeTrue();
        repeated.Scopes.Single().RetiredAt.ShouldEqual(Now);
    }

    [Fact]
    void should_consume_no_public_publication_contracts()
    {
        var eventTypes = typeof(ResourceRegistrationReducer).GetMethods().Where(method => method.Name == "On").Select(method => method.GetParameters()[0].ParameterType).ToHashSet();
        eventTypes.SetEquals([typeof(ResourceRegistrationRecorded), typeof(ResourceScopeRegistrationRecorded), typeof(ResourceScopeRetirementRecorded)]).ShouldBeTrue();
    }
}

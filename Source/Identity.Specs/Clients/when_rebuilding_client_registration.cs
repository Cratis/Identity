// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Chronicle.Events;
using Cratis.Identity.Contracts.Clients;
using Cratis.Identity.Contracts.Clients.Events;

namespace Cratis.Identity.Clients;

public class when_rebuilding_client_registration : given.client_commands
{
    [Fact]
    void should_admit_the_protected_client_reducer() => DecisionReadAdmissions.ShouldAdmitDecisionReadsIn(typeof(ClientRegistration).Assembly);

    [Theory]
    [InlineData(0)]
    [InlineData(60)]
    void should_fold_only_internal_configuration_and_digest_revisions(int overlapSeconds)
    {
        var reducer = new ClientRegistrationReducer();
        var context = EventContext.EmptyWithEventSourceId(_id.Value);
        var registered = reducer.On(_configuration.Registered(new("spec-client"), Now), null, context);
        registered.ClientId.Value.ShouldEqual("spec-client");
        registered.Configuration.Scopes.ShouldEqual(_configuration.Scopes);
        var redirected = reducer.On(new ClientRedirectUrisChanged(_configuration.RedirectUris), registered, context);
        redirected.Configuration.RedirectUris.ShouldEqual(_configuration.RedirectUris);
        var first = reducer.On(new ClientSecretRotated(ClientSecretId.New(), _active.SecretHash, Now, Now), redirected, context);
        var nextHash = _digester.Digest(Plaintext);
        var rotated = reducer.On(new ClientSecretRotated(ClientSecretId.New(), nextHash, Now.AddDays(1), Now.AddDays(1).AddSeconds(overlapSeconds)), first, context);
        rotated.SecretHash.ShouldEqual(nextHash);
        rotated.PreviousSecretHash.ShouldEqual(overlapSeconds == 0 ? null : first.SecretHash);
        var retired = reducer.On(new ClientRetired("owner_retired", Now.AddDays(2)), rotated, context);
        retired.Id.ShouldEqual(_id.Value);
        retired.ClientId.ShouldEqual(registered.ClientId);
        retired.Retired.ShouldBeTrue();
        JsonSerializer.Serialize(retired).Contains(Plaintext, StringComparison.Ordinal).ShouldBeFalse();
        retired.ToString().Contains(Plaintext, StringComparison.Ordinal).ShouldBeFalse();
    }
}

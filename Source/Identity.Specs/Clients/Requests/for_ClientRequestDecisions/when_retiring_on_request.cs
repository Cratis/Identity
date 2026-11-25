// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Cratis.Identity.Contracts.Clients.Events;

namespace Cratis.Identity.Clients.Requests.for_ClientRequestDecisions;

public class when_retiring_on_request : given.client_commands
{
    static readonly string[] _owners = ["owner-1"];

    ClientRequestOutcome _accepted;
    ClientRequestOutcome _notOwner;
    ClientRequestOutcome _unknown;
    ClientRequestOutcome _alreadyRetired;

    void Because()
    {
        _accepted = Decide(_active, "owner-1");
        _notOwner = Decide(_active, "someone-else");
        _unknown = Decide(null, "owner-1");
        _alreadyRetired = Decide(_active with { Retired = true }, "owner-1");
    }

    [Fact] void should_retire_for_a_security_owner() => _accepted.Events.Select(_ => _.Event.GetType()).ShouldContainOnly(typeof(ClientRetired));
    [Fact] void should_retire_the_named_registration() => _accepted.Events.Single().EventSourceId.Value.ShouldEqual(_active.Id.ToString());
    [Fact] void should_refuse_anyone_who_is_not_a_security_owner() => _notOwner.Refusal.ShouldEqual("requester_not_security_owner");
    [Fact] void should_refuse_a_registration_that_does_not_exist() => _unknown.Refusal.ShouldEqual("client_not_active");
    [Fact] void should_refuse_a_client_that_is_already_retired() => _alreadyRetired.Refusal.ShouldEqual("client_not_active");

    ClientRequestOutcome Decide(ClientRegistration? registration, string by) =>
        ClientRequestDecisions.ForRetirement(new(new(_active.Id), by), _owners, registration, Now);
}

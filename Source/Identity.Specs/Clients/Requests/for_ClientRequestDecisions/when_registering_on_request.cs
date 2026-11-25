// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Cratis.Identity.Contracts.Clients;
using Cratis.Identity.Contracts.Clients.Events;

namespace Cratis.Identity.Clients.Requests.for_ClientRequestDecisions;

public class when_registering_on_request : given.client_commands
{
    static readonly string[] _owners = ["owner-1"];
    static readonly HashSet<string> _reserved = new(StringComparer.Ordinal) { "cratis-cli" };
    static readonly HashSet<string> _scopes = new(StringComparer.Ordinal) { "openid", "mcp" };

    ClientRequestOutcome _accepted;
    ClientRequestOutcome _notOwner;
    ClientRequestOutcome _reservedId;
    ClientRequestOutcome _localhost;
    ClientRequestOutcome _unknownScope;
    ClientRequestOutcome _badId;

    void Because()
    {
        _accepted = Decide(Request("my-agent", ["http://127.0.0.1/callback"], ["openid"], "owner-1"));
        _notOwner = Decide(Request("my-agent", ["http://127.0.0.1/callback"], ["openid"], "someone-else"));
        _reservedId = Decide(Request("cratis-cli", ["http://127.0.0.1/callback"], ["openid"], "owner-1"));
        _localhost = Decide(Request("my-agent", ["http://localhost/callback"], ["openid"], "owner-1"));
        _unknownScope = Decide(Request("my-agent", ["http://127.0.0.1/callback"], ["not-a-scope"], "owner-1"));
        _badId = Decide(Request("bad:id", ["http://127.0.0.1/callback"], ["openid"], "owner-1"));
    }

    [Fact] void should_accept_a_security_owners_request() => _accepted.Accepted.ShouldBeTrue();
    [Fact] void should_register_the_client_and_its_redirects() => _accepted.Events.Select(_ => _.Event.GetType()).ShouldContainOnly(typeof(ClientRegistered), typeof(ClientRedirectUrisChanged));
    [Fact] void should_keep_both_facts_on_one_stream() => _accepted.Events.Select(_ => _.EventSourceId.Value).Distinct().Count().ShouldEqual(1);
    [Fact] void should_register_a_public_client() => ((ClientRegistered)_accepted.Events[0].Event).ClientType.ShouldEqual(ClientType.Public);
    [Fact] void should_refuse_anyone_who_is_not_a_security_owner() => _notOwner.Refusal.ShouldEqual("requester_not_security_owner");
    [Fact] void should_append_nothing_for_a_refusal() => _notOwner.Events.ShouldBeEmpty();
    [Fact] void should_refuse_an_id_the_configuration_declares() => _reservedId.Refusal.ShouldEqual("client_id_already_reserved");
    [Fact] void should_refuse_a_localhost_redirect() => _localhost.Refusal.ShouldEqual("invalid_client_configuration");
    [Fact] void should_refuse_a_scope_that_is_not_served() => _unknownScope.Refusal.ShouldEqual("unknown_scope");
    [Fact] void should_refuse_an_invalid_client_id() => _badId.Refusal.ShouldEqual("invalid_client_registration");

    static ClientRegistrationRequested Request(string clientId, string[] redirects, string[] scopes, string by) => new(clientId, redirects, scopes, by);

    ClientRequestOutcome Decide(ClientRegistrationRequested request) =>
        ClientRequestDecisions.ForRegistration(request, _owners, _reserved, _scopes, new(Guid.NewGuid()), Now);
}

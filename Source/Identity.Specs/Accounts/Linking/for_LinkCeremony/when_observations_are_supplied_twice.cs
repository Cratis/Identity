// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Identity.Accounts.Linking.for_LinkCeremony;

public class when_observations_are_supplied_twice : Specification
{
    readonly LinkCeremony _ceremony = new();
    readonly ClaimsPrincipal _session = new();
    Exception _exception;

    void Establish() => _ceremony.Set(_session, null, new(true), CancellationToken.None);

    void Because() => _exception = Catch.Exception(() => _ceremony.Set(new(), null, new(false), CancellationToken.None));

    [Fact] void should_refuse_replacement() => _exception.ShouldBeOfExactType<AccountOperationRefused>();
    [Fact] void should_preserve_the_reason() => _exception.Message.ShouldEqual("link_ceremony_already_supplied");
    [Fact] void should_keep_the_original_session() => _ceremony.Session.ShouldEqual(_session);
    [Fact] void should_keep_the_original_intent() => _ceremony.Intent.Explicit.ShouldBeTrue();
}

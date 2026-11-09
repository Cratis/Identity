// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Revocation;

public class when_chronicle_credentials_reuse_a_realm_secret : Specification
{
    const string Shared = "synthetic-shared-value";

    Exception _reused;
    Exception _distinct;
    Exception _unparsable;

    void Because()
    {
        _reused = Catch.Exception(() => ChronicleCredentialSeparation.Require($"chronicle://synthetic-client:{Shared}@host.example:35000", ["synthetic-other", Shared]));
        _distinct = Catch.Exception(() => ChronicleCredentialSeparation.Require("chronicle://synthetic-client:synthetic-chronicle-value@host.example:35000", [Shared, null, string.Empty]));
        _unparsable = Catch.Exception(() => ChronicleCredentialSeparation.Require("not a uri", [Shared]));
    }

    [Fact] void should_refuse_a_reused_secret() => _reused.ShouldBeOfExactType<RevocationPending>();
    [Fact] void should_accept_distinct_secrets_and_ignore_empty_ones() => _distinct.ShouldBeNull();
    [Fact] void should_not_treat_an_unparsable_connection_as_reuse() => _unparsable.ShouldBeNull();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts;
using Cratis.Identity.Accounts.Lookup;
using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Upstream.for_VerifiedKeyGate;

public class when_checking_a_verified_key : Specification
{
    VerifiedAuthentication _authentication;
    VerifiedSignInKey _key;
    VerifiedKeyGate _gate;

    void Establish()
    {
        var keys = Substitute.For<IUpstreamLookupKeys>();
        keys.Current.Returns(new UpstreamLookupKey(7, Enumerable.Range(0, 32).Select(value => (byte)value).ToArray()));
        var digester = new UpstreamKeyDigester(keys);
        _authentication = new GitHubConnection(GitHubConnection.FixedConnectionId, TimeProvider.System).From(new GitHubUser(12345, "display", "Person"), FactId.New());
        _gate = new(keys, digester);
        _key = _gate.KeyFor(_authentication);
    }

    [Fact] void should_accept_the_adapter_verified_pair() => _gate.Validate(_authentication, _key).ShouldBeNull();

    [Theory]
    [InlineData("connection")]
    [InlineData("issuer")]
    [InlineData("subject")]
    [InlineData("revision")]
    [InlineData("digest")]
    public void should_reject_any_mismatched_provenance(string changed)
    {
        var key = changed switch
        {
            "connection" => _key with { ConnectionId = new(Guid.NewGuid()) },
            "issuer" => _key with { IdpIssuer = "https://other.example.test" },
            "subject" => _key with { IdpSubject = "different" },
            "revision" => _key with { Revision = _key.Revision + 1 },
            _ => _key with { Digest = "invalid" }
        };
        _gate.Validate(_authentication, key).Message.ShouldEqual("verified_upstream_key_mismatch");
    }
}

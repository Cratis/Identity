// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Clients.Secrets;
using Cratis.Identity.Contracts;
using Cratis.Identity.Contracts.Clients;

namespace Cratis.Identity.Clients.for_ClientSecretDigester;

public class when_matching_hostile_comparands
{
    readonly ClientSecretDigester _digester = new(given.client_test_keys.Create());
    readonly ClientId _id = ClientId.Create("test-client");

    [Fact] void should_accept_the_current_secret() => _digester.Matches(_id, "current", _digester.Digest(_id, "current")).ShouldBeTrue();
    [Fact] void should_refuse_the_wrong_secret() => _digester.Matches(_id, "wrong", _digester.Digest(_id, "current")).ShouldBeFalse();
    [Fact] void should_refuse_another_client() => _digester.Matches(ClientId.Create("other-client"), "current", _digester.Digest(_id, "current")).ShouldBeFalse();
    [Fact] void should_refuse_another_revision() => _digester.Matches(_id, "current", new ClientSecretDigester(given.client_test_keys.Create(2)).Digest(_id, "current")).ShouldBeFalse();

    [Theory]
    [InlineData("")]
    [InlineData("v1.not-base64!")]
    [InlineData("AQAAAAIAAYagAAAAEFakeLegacyPBKDF2Comparand")]
    [InlineData("x1.AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    void should_refuse_malformed_or_legacy_values(string value) => _digester.Matches(_id, "current", new SecretDigest(value)).ShouldBeFalse();
}

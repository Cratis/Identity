// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts.Lookup;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Upstream;

namespace Cratis.Identity.Accounts.for_UpstreamSubject;

public class when_validating_new_upstream_observations
{
    public static TheoryData<string> InvalidSubjects => new()
    {
        string.Empty,
        " ",
        "immutable\nsubject",
        new string('a', 257),
        new string('é', 129)
    };

    [Theory]
    [MemberData(nameof(InvalidSubjects))]
    void should_refuse_invalid_subjects_in_the_factory(string value) =>
        Catch.Exception(() => UpstreamSubject.Create(value)).ShouldBeOfExactType<InvalidAccountValue>();

    [Theory]
    [MemberData(nameof(InvalidSubjects))]
    void should_refuse_invalid_subjects_in_implicit_conversion(string value) =>
        Catch.Exception(() => _ = (UpstreamSubject)value).ShouldBeOfExactType<InvalidAccountValue>();

    [Theory]
    [MemberData(nameof(InvalidSubjects))]
    void should_not_accept_replayed_values_as_sign_in_or_linking_proof(string value) =>
        Catch.Exception(() => _ = new VerifiedAuthentication(GitHubConnection.FixedConnectionId, "https://github.com", new UpstreamSubject(value), DateTimeOffset.UnixEpoch, FactId.New()))
            .ShouldBeOfExactType<InvalidAccountValue>();

    [Theory]
    [MemberData(nameof(InvalidSubjects))]
    void should_not_digest_invalid_replayed_values_for_lookup(string value) =>
        Catch.Exception(() => new UpstreamKeyDigester(Substitute.For<IUpstreamLookupKeys>()).Digest("https://github.com", new UpstreamSubject(value)))
            .ShouldBeOfExactType<InvalidAccountValue>();
}

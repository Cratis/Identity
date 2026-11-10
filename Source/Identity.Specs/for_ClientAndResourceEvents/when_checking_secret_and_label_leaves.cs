// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;
using Cratis.Identity.Contracts.Check;
using Cratis.Identity.Contracts.Clients;
using Cratis.Identity.Contracts.Compliance;

namespace Cratis.Identity.for_ClientAndResourceEvents;

public class when_checking_secret_and_label_leaves : Specification
{
    [Theory]
    [InlineData(typeof(RawSecret), "ClientSecret", "forbidden_member_name")]
    [InlineData(typeof(RawHash), "ClientSecretHash", "unapproved_safe_concept")]
    [InlineData(typeof(ReversibleHash), "ClientSecretHash", "forbidden_payload_type")]
    [InlineData(typeof(UnreviewedHash), "ClientSecretHash", "unapproved_safe_concept")]
    [InlineData(typeof(ExtendedDigest), "ClientSecretHash", "unapproved_safe_concept")]
    [InlineData(typeof(PlainLabel), "DisplayName", "missing_pii")]
    [InlineData(typeof(MisnamedLabel), "ClientSecret", "unapproved_safe_concept")]
    public void should_not_allow_secret_or_personal_data_by_renaming(Type type, string path, string reason) =>
        ContentRules.Check(ReflectionShapeBuilder.Build(type), SafeConceptRegistry.Entries).ShouldContain(new Violation(path, reason));

    [Fact]
    void should_allow_only_the_internal_reviewed_business_label() =>
        ContentRules.Check(ReflectionShapeBuilder.Build(typeof(ReviewedLabel)), SafeConceptRegistry.Entries).ShouldBeEmpty();

    [Fact]
    void should_not_exempt_public_business_labels()
    {
        var shape = ReflectionShapeBuilder.Build(typeof(ReviewedLabel)) with { Visibility = "Public" };
        ContentRules.Check(shape, SafeConceptRegistry.Entries).ShouldContain(new Violation("DisplayName", "missing_pii"));
    }

    internal sealed record RawSecret(string ClientSecret);
    internal sealed record RawHash(string ClientSecretHash);
    internal sealed record ReversibleHash(byte[] ClientSecretHash);
    internal sealed record UnreviewedHash(UnreviewedDigest ClientSecretHash);
    internal sealed record UnreviewedDigest(string Value) : ConceptAs<string>(Value);
    internal sealed record ExtendedDigest(ExtraDigest ClientSecretHash);
    internal sealed record ExtraDigest(string Value, string Secret) : ConceptAs<string>(Value);
    internal sealed record PlainLabel(string DisplayName);
    internal sealed record ReviewedLabel(ClientDisplayName DisplayName);
    internal sealed record MisnamedLabel(ClientDisplayName ClientSecret);
}

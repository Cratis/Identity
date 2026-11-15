// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Identity.for_CredentialRecordVectors;

/// <summary>
/// Checks the shape of the credential-record data vectors. These are data only: the specs do not replay them
/// against a gateway, and so are not runtime conformance evidence.
/// </summary>
public class when_reading_the_credential_record_vectors
{
    static readonly string[] _required =
    [
        "active-unbound-accept",
        "revoked-inactive-refuse",
        "legacy-no-credential-id-refuse",
        "bound-with-valid-proof-accept",
        "bound-without-proof-refuse",
        "unbound-token-with-proof-refuse"
    ];

    static readonly string[] _all =
    [
        .. _required,
        "cid-mismatch-refuse",
        "unknown-credential-kind-refuse",
        "missing-sender-binding-result-refuse"
    ];

    public static TheoryData<string> Vectors => [.. _all];

    [Fact]
    void should_include_every_vector_required_by_the_issue()
    {
        foreach (var name in _required)
        {
            File.Exists(PathOf(name)).ShouldBeTrue();
        }
    }

    [Fact]
    void should_not_contain_vectors_outside_the_known_set()
    {
        var found = Directory.GetFiles(Path.GetDirectoryName(PathOf(_all[0])), "*.json").Select(Path.GetFileNameWithoutExtension).Order();
        found.ShouldEqual(_all.Order());
    }

    [Theory]
    [MemberData(nameof(Vectors))]
    internal void should_be_named_after_its_file_and_describe_given_when_then(string vector)
    {
        using var fixture = Read(vector);
        var root = fixture.RootElement;

        root.GetProperty("name").GetString().ShouldEqual(vector);
        string.IsNullOrWhiteSpace(root.GetProperty("description").GetString()).ShouldBeFalse();
        root.GetProperty("given").ValueKind.ShouldEqual(JsonValueKind.Object);
        root.GetProperty("when").GetProperty("action").GetString().ShouldEqual("build_oauth_envelope_at_authproxy");
        root.GetProperty("then").ValueKind.ShouldEqual(JsonValueKind.Object);
    }

    [Theory]
    [MemberData(nameof(Vectors))]
    internal void should_have_an_accept_or_refuse_outcome_matching_its_name(string vector)
    {
        using var fixture = Read(vector);

        fixture.RootElement.GetProperty("then").GetProperty("outcome").GetString().ShouldEqual(vector.EndsWith("-accept") ? "accept" : "refuse");
    }

    [Theory]
    [InlineData("active-unbound-accept")]
    [InlineData("bound-with-valid-proof-accept")]
    internal void should_accept_only_an_oauth_grant_with_a_credential_id_that_is_not_the_token_jti(string vector)
    {
        using var fixture = Read(vector);
        var then = fixture.RootElement.GetProperty("then");
        var credentialId = then.GetProperty("credential_id").GetString();

        then.GetProperty("credential_kind").GetString().ShouldEqual("oauth-grant");
        Guid.TryParseExact(credentialId, "D", out _).ShouldBeTrue();
        credentialId.ShouldEqual(fixture.RootElement.GetProperty("given").GetProperty("introspection_response").GetProperty("credential_id").GetString());
        if (fixture.RootElement.GetProperty("given").TryGetProperty("token_jti", out var tokenJti))
        {
            credentialId.ShouldNotEqual(tokenJti.GetString());
        }
    }

    [Fact]
    void should_report_the_proof_jti_and_not_the_token_jti_for_a_bound_acceptance()
    {
        using var fixture = Read("bound-with-valid-proof-accept");
        var given = fixture.RootElement.GetProperty("given");
        var binding = fixture.RootElement.GetProperty("then").GetProperty("sender_binding");

        binding.GetProperty("jti").GetString().ShouldEqual(given.GetProperty("proof").GetProperty("jti").GetString());
        binding.GetProperty("jti").GetString().ShouldNotEqual(given.GetProperty("token_jti").GetString());
        binding.GetProperty("jkt").GetString().ShouldEqual(given.GetProperty("introspection_response").GetProperty("cnf").GetProperty("jkt").GetString());
    }

    [Fact]
    void should_introspect_a_revoked_token_as_exactly_inactive()
    {
        using var fixture = Read("revoked-inactive-refuse");
        var response = fixture.RootElement.GetProperty("given").GetProperty("introspection_response");

        response.EnumerateObject().Select(_ => _.Name).ShouldEqual(["active"]);
        response.GetProperty("active").GetBoolean().ShouldBeFalse();
    }

    static string PathOf(string vector) => Path.Combine(AppContext.BaseDirectory, "Vectors", "identity-credential-record-v1", vector + ".json");

    static JsonDocument Read(string vector) => JsonDocument.Parse(File.ReadAllText(PathOf(vector)));
}

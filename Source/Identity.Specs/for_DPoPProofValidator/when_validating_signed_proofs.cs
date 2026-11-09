// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.DPoP;

namespace Cratis.Identity.for_DPoPProofValidator;

public class when_validating_signed_proofs : given.a_signed_proof
{
    [Fact] void should_accept_a_real_es256_signature() => _validator.Validate([Sign()], Endpoint).Proof.ShouldNotBeNull();
    [Theory]
    [InlineData("none")]
    [InlineData("HS256")]
    [InlineData("RS256")]
    [InlineData("ES384")]
    [InlineData("EdDSA")]
    [InlineData("es256")]
    [InlineData("")]
    void should_refuse_every_other_algorithm(string algorithm)
    {
        WithHeader("alg", algorithm).ShouldEqual(DPoPFailure.UnsupportedAlgorithm);
    }

    [Theory]
    [InlineData("https://internal/connect/token")]
    [InlineData("https://identity.example/connect/token?x=1")]
    [InlineData("https://identity.example/connect/token#x")]
    [InlineData("https://identity.example/connect/token/")]
    [InlineData("https://IDENTITY.example/connect/token")]
    [InlineData("https://identity.example:443/connect/token")]
    void should_not_normalize_the_target(string target)
    {
        WithPayload("htu", target).ShouldEqual(DPoPFailure.HtuMismatch);
    }

    [Theory]
    [InlineData(-300, false)]
    [InlineData(-299, true)]
    [InlineData(30, true)]
    [InlineData(31, false)]
    void should_enforce_both_time_boundaries(int delta, bool accepted)
    {
        (WithPayload("iat", Now + delta) == DPoPFailure.None).ShouldEqual(accepted);
    }

    [Theory]
    [InlineData("d")]
    [InlineData("k")]
    [InlineData("jku")]
    [InlineData("x5u")]
    [InlineData("x5c")]
    void should_refuse_private_or_remote_key_material(string member)
    {
        WithKey(member, "synthetic").ShouldEqual(DPoPFailure.InvalidJwk);
    }

    [Fact] void should_refuse_duplicate_payload_members() => _validator.Validate([Sign(payload: /*lang=json*/ "{\"jti\":\"a\",\"jti\":\"b\"}")], Endpoint).Failure.ShouldEqual(DPoPFailure.Malformed);
    [Fact] void should_refuse_duplicate_header_members() => _validator.Validate([Sign(header: /*lang=json*/ "{\"alg\":\"ES256\",\"alg\":\"none\"}")], Endpoint).Failure.ShouldEqual(DPoPFailure.Malformed);
    [Fact] void should_refuse_multiple_headers() => _validator.Validate([Sign(), Sign()], Endpoint).Failure.ShouldEqual(DPoPFailure.Malformed);
    [Fact] void should_refuse_oversize_headers() => _validator.Validate([new string('a', 4097)], Endpoint).Failure.ShouldEqual(DPoPFailure.Malformed);
    [Fact] void should_refuse_a_missing_proof() => _validator.Validate([], Endpoint).Failure.ShouldEqual(DPoPFailure.MissingProof);

    [Fact]
    void should_refuse_critical_headers()
    {
        WithHeader("crit", Array.Empty<string>()).ShouldEqual(DPoPFailure.Malformed);
    }

    [Fact]
    void should_refuse_wrong_type()
    {
        WithHeader("typ", "JWT").ShouldEqual(DPoPFailure.Malformed);
    }

    [Fact]
    void should_require_exact_method()
    {
        WithPayload("htm", "post").ShouldEqual(DPoPFailure.HtmMismatch);
    }

    [Fact]
    void should_refuse_fractional_iat()
    {
        WithPayload("iat", Now + 0.5).ShouldEqual(DPoPFailure.IatOutsideWindow);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(257)]
    void should_bound_the_proof_identifier(int length)
    {
        WithPayload("jti", new string('a', length)).ShouldEqual(DPoPFailure.InvalidJti);
    }

    [Fact]
    void should_verify_the_signature()
    {
        var parts = Sign().Split('.');
        parts[2] = new string('A', 86);
        _validator.Validate([string.Join('.', parts)], Endpoint).Failure.ShouldEqual(DPoPFailure.InvalidSignature);
    }

    [Theory]
    [InlineData("use", "enc")]
    [InlineData("alg", "ES384")]
    [InlineData("crv", "P-384")]
    [InlineData("kty", "oct")]
    void should_require_a_signature_verification_key(string member, string value) => WithKey(member, value).ShouldEqual(DPoPFailure.InvalidJwk);

    [Fact] void should_refuse_signing_key_operations() => WithKey("key_ops", new[] { "sign" }).ShouldEqual(DPoPFailure.InvalidJwk);

    [Fact] void should_accept_verification_key_operations() => WithKey("key_ops", new[] { "verify" }).ShouldEqual(DPoPFailure.None);

    [Fact]
    void should_compute_the_rfc7638_thumbprint_independently() => _validator.Validate([Sign()], Endpoint).Proof.Jkt.ShouldEqual(Microsoft.IdentityModel.Tokens.Base64UrlEncoder.Encode(new Microsoft.IdentityModel.Tokens.ECDsaSecurityKey(_key).ComputeJwkThumbprint()));

    [Fact]
    void should_refuse_off_curve_points()
    {
        WithKey("x", new string('A', 43)).ShouldEqual(DPoPFailure.InvalidJwk);
    }
}

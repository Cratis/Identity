// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cratis.Identity.Authorization.TokenExchange;
using Cratis.Identity.given;
using Microsoft.IdentityModel.Tokens;

namespace Cratis.Identity.for_SubjectTokenValidator;

public class when_validating_numeric_dates : given.a_subject_token_validator
{
    public static TheoryData<string, string> MalformedDates
    {
        get
        {
            var data = new TheoryData<string, string>();
            foreach (var claim in new[] { "exp", "nbf", "iat" })
            {
                foreach (var value in new[] { "1e309", "-1e309", "1e9999", "-1e-9999", "-1", "-0.5", "253402300800", "253402300799.5", "9223372036854775807", "\"1700000000\"", "true", "false", "{}", "[]", "null" })
                {
                    data.Add(claim, value);
                }
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(MalformedDates))]
    async Task should_refuse_malformed_dates_before_fetching_keys(string claim, string rawValue)
    {
        (await RefusalOf(Sign(claim, rawValue))).ShouldEqual(SubjectTokenRefusal.Malformed);
        await keys.DidNotReceiveWithAnyArgs().Get(default!, default, TestContext.Current.CancellationToken);
    }

    [Fact]
    async Task should_preserve_fractional_issued_at_and_expiry()
    {
        var payload = Payload();
        var issuedAt = time.Now.ToUnixTimeSeconds() - 10 + 0.5;
        var expires = time.Now.ToUnixTimeSeconds() + 300 + 0.75;
        payload["iat"] = issuedAt;
        payload["exp"] = expires;
        payload["nbf"] = issuedAt;
        var result = await Validate(ExternalIssuerFixture.Sign(payload, issuer.RsaCredentials));
        result.Refusal.ShouldEqual(SubjectTokenRefusal.None);
        result.Subject.ShouldNotBeNull();
        result.Subject.IssuedAt.ShouldEqual(DateTimeOffset.UnixEpoch.AddSeconds(issuedAt));
        result.Subject.Expiry.ShouldEqual(DateTimeOffset.UnixEpoch.AddSeconds(expires));
    }

    [Fact]
    async Task should_not_truncate_fractional_expiry_during_lifetime_validation()
    {
        time.Now = DateTimeOffset.FromUnixTimeSeconds(time.Now.ToUnixTimeSeconds()).AddMilliseconds(250);
        IssuerOptions.ClockSkew = TimeSpan.Zero;
        (await RefusalOf(Sign("exp", $"{time.Now.ToUnixTimeSeconds()}.5"))).ShouldEqual(SubjectTokenRefusal.None);
    }

    [Fact]
    async Task should_not_truncate_fractional_not_before_during_lifetime_validation()
    {
        time.Now = DateTimeOffset.FromUnixTimeSeconds(time.Now.ToUnixTimeSeconds()).AddMilliseconds(250);
        IssuerOptions.ClockSkew = TimeSpan.Zero;
        (await RefusalOf(Sign("nbf", $"{time.Now.ToUnixTimeSeconds()}.5"))).ShouldEqual(SubjectTokenRefusal.Invalid);
    }

    [Theory]
    [InlineData("exp", "0", SubjectTokenRefusal.Invalid)]
    [InlineData("exp", "253402300799", SubjectTokenRefusal.None)]
    [InlineData("nbf", "0", SubjectTokenRefusal.None)]
    [InlineData("nbf", "-0.0e-9999", SubjectTokenRefusal.None)]
    [InlineData("nbf", "253402300799", SubjectTokenRefusal.Invalid)]
    [InlineData("iat", "0", SubjectTokenRefusal.Stale)]
    [InlineData("iat", "253402300799", SubjectTokenRefusal.Stale)]
    async Task should_accept_the_numeric_range_boundaries_before_applying_lifetime_rules(string claim, string rawValue, SubjectTokenRefusal expected)
    {
        (await RefusalOf(Sign(claim, rawValue))).ShouldEqual(expected);
    }

    string Sign(string claim, string rawValue)
    {
        var payload = Payload();
        payload.Remove(claim);
        var json = JsonSerializer.Serialize(payload).TrimEnd('}') + $",\"{claim}\":{rawValue}}}";
        var header = JsonSerializer.Serialize(new { alg = "RS256", typ = "JWT", kid = ExternalIssuerFixture.RsaKeyId });
        var input = $"{Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes(header))}.{Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes(json))}";
        var rsa = ((RsaSecurityKey)issuer.RsaCredentials.Key).Rsa;
        var signature = rsa.SignData(Encoding.ASCII.GetBytes(input), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return $"{input}.{Base64UrlEncoder.Encode(signature)}";
    }
}

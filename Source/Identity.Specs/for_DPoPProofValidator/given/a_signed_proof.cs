// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cratis.Identity.DPoP;
using Microsoft.IdentityModel.Tokens;

namespace Cratis.Identity.for_DPoPProofValidator.given;

public class a_signed_proof : Specification
{
    protected const string Endpoint = "https://identity.example/connect/token";
    protected const long Now = 2000000000;
    protected ECDsa _key;
    protected Dictionary<string, object> _header;
    protected Dictionary<string, object> _payload;
    private protected DPoPProofValidator _validator;

    void Establish()
    {
        _key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var point = _key.ExportParameters(false).Q;
        _header = new() { ["typ"] = "dpop+jwt", ["alg"] = "ES256", ["jwk"] = new Dictionary<string, object> { ["kty"] = "EC", ["crv"] = "P-256", ["x"] = Base64UrlEncoder.Encode(point.X), ["y"] = Base64UrlEncoder.Encode(point.Y) } };
        _payload = new() { ["jti"] = "proof-1", ["htm"] = "POST", ["htu"] = Endpoint, ["iat"] = Now };
        _validator = new DPoPProofValidator(new Clock());
    }

    protected string Sign(string? header = null, string? payload = null)
    {
        var data = Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes(header ?? JsonSerializer.Serialize(_header))) + "." + Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes(payload ?? JsonSerializer.Serialize(_payload)));
        return data + "." + Base64UrlEncoder.Encode(_key.SignData(Encoding.ASCII.GetBytes(data), HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation));
    }

    private protected DPoPFailure WithHeader(string member, object value)
    {
        var header = new Dictionary<string, object>(_header) { [member] = value };
        return _validator.Validate([Sign(header: JsonSerializer.Serialize(header))], Endpoint).Failure;
    }

    private protected DPoPFailure WithPayload(string member, object value)
    {
        var payload = new Dictionary<string, object>(_payload) { [member] = value };
        return _validator.Validate([Sign(payload: JsonSerializer.Serialize(payload))], Endpoint).Failure;
    }

    private protected DPoPFailure WithKey(string member, object value)
    {
        var key = new Dictionary<string, object>((Dictionary<string, object>)_header["jwk"]) { [member] = value };
        return WithHeader("jwk", key);
    }

    void Destroy() => _key.Dispose();

    sealed class Clock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => DateTimeOffset.FromUnixTimeSeconds(Now);
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using Cratis.Identity.given;
using Cratis.Identity.Sessions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.for_EndSessionHintValidator.given;

public class a_hint_validator : Specification
{
    protected const string Issuer = "https://identity.example/";
    protected const string ClientId = "oidc-client";
    protected const string Subject = "7d9d5c9e-0000-4000-8000-000000000001";
    protected const string SessionId = "session-one";
    protected ExternalIssuerFixture keys;
    protected FixedTimeProvider time;
    protected OpenIddictServerOptions server;
    private protected IOpenIddictApplicationManager applications;
    private protected EndSessionHintValidator validator;
    protected object application;

    void Establish()
    {
        keys = new();
        time = new(DateTimeOffset.FromUnixTimeSeconds(1_700_000_000));
        server = new() { Issuer = new(Issuer) };
        server.SigningCredentials.Add(keys.RsaCredentials);
        var monitor = Substitute.For<IOptionsMonitor<OpenIddictServerOptions>>();
        monitor.CurrentValue.Returns(server);
        application = new();
        applications = Substitute.For<IOpenIddictApplicationManager>();
        applications.FindByClientIdAsync(ClientId, Arg.Any<CancellationToken>()).Returns(application);
        applications.GetPermissionsAsync(application, Arg.Any<CancellationToken>()).Returns(new ValueTask<ImmutableArray<string>>([Permissions.Prefixes.Scope + Scopes.OpenId]));
        validator = new(monitor, applications, Options.Create(new EndSessionOptions()), time);
    }

    protected Dictionary<string, object> Payload() => new()
    {
        ["iss"] = Issuer,
        ["aud"] = ClientId,
        ["sub"] = Subject,
        ["sid"] = SessionId,
        ["iat"] = time.Now.AddMinutes(-5).ToUnixTimeSeconds(),
        ["exp"] = time.Now.AddMinutes(5).ToUnixTimeSeconds(),
        ["auth_time"] = time.Now.AddMinutes(-5).ToUnixTimeSeconds()
    };

    protected string Sign(Action<Dictionary<string, object>>? change = null)
    {
        var payload = Payload();
        change?.Invoke(payload);
        return ExternalIssuerFixture.Sign(payload, keys.RsaCredentials);
    }

    protected string WithHeader(Dictionary<string, object> header)
    {
        var input = $"{Base64UrlEncoder.Encode(JsonSerializer.Serialize(header))}.{Base64UrlEncoder.Encode(JsonSerializer.Serialize(Payload()))}";
        var credentials = keys.RsaCredentials;
        var provider = credentials.Key.CryptoProviderFactory.CreateForSigning(credentials.Key, credentials.Algorithm);
        try
        {
            return $"{input}.{Base64UrlEncoder.Encode(provider.Sign(Encoding.UTF8.GetBytes(input)))}";
        }
        finally
        {
            credentials.Key.CryptoProviderFactory.ReleaseSignatureProvider(provider);
        }
    }

    protected async Task<bool> Refused(string token) => await validator.Validate(token) is null;

    void Destroy() => keys.Dispose();
}

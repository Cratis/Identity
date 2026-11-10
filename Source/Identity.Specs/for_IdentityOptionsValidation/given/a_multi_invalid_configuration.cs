// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.for_IdentityOptionsValidation.given;

public class a_multi_invalid_configuration : Specification
{
    protected IdentityOptions options;
    protected ValidateOptionsResult result;

    void Establish()
    {
        options = new IdentityOptions
        {
            SecurityOwners = [""],
            Issuer = "https://identity.example/path",
            Resources = [Resource("b", "bad b", "ftp://example/b"), Resource("a", "bad a", "http://example/a?query=1"), Resource("b", "bad b", "ftp://example/b"), Resource("a", "bad a", "http://example/a?query=1"), new()],
            IntrospectionClients = [new() { ClientId = "b" }, new() { ClientId = "a" }, new() { ClientId = "b" }, new() { ClientId = "a" }]
        };
        options.SecretStore.ReloadInterval = TimeSpan.Zero;
        options.Tokens.AccessTokenLifetime = TimeSpan.Zero;
        options.DPoP.Enabled = true;
        options.DPoP.PublicTokenEndpoint = "http://localhost/token";
        options.DPoP.RequiredClients = ["unknown"];
        options.DPoP.ProofLifetime = TimeSpan.Zero;
        options.DeviceGrant.UserCodeLength = 0;
        options.DeviceGrant.UserCodeCharset = "aa";
        options.DeviceGrant.CodeLifetime = TimeSpan.Zero;
        options.DeviceGrant.PollingInterval = TimeSpan.FromSeconds(5.5);
        options.DeviceGrant.VerificationWindow = TimeSpan.Zero;
        options.DeviceGrant.AttemptsPerCode = 0;
        options.TokenExchange.Issuers = [new()];
        options.TokenExchange.Clients = [new()];
        options.TokenRateLimit.Enabled = true;
        options.TokenRateLimit.Window = TimeSpan.Zero;
        options.TokenRateLimit.PerSourceRequests = 0;
        options.Accounts.ReadModelCatchUpTimeout = TimeSpan.Zero;
        options.Accounts.LookupKeyRevision = 0;
        options.Accounts.LookupKey = "invalid";
    }

    protected void Validate(string environment) => result = new IdentityOptionsValidation(new HostingEnvironment { EnvironmentName = environment }).Validate(null, options);

    protected static IEnumerable<string> Expected(bool development)
    {
        var failures = new List<string>
        {
            "Identity:SecurityOwners must contain only non-empty subjects.",
            "Issuer \"https://identity.example/path\" must be the root of its origin and end with a slash.",
            "The secret store reload interval must be between one second and five minutes.",
            "Identity:Tokens:AccessTokenLifetime must be positive and no greater than 00:10:00.",
            "The audience \"b\" is configured more than once.",
            "The audience \"a\" is configured more than once.",
            "The resource indicator \"bad-b\" is configured more than once.",
            "The resource indicator \"bad-a\" is configured more than once.",
            "The scope \"bad b\" is configured more than once.",
            "The scope \"bad a\" is configured more than once.",
            "The introspection client \"b\" is configured more than once.",
            "The introspection client \"a\" is configured more than once."
        };
        foreach (var (audience, scope) in new[] { ("b", "bad b"), ("a", "bad a"), ("b", "bad b"), ("a", "bad a") })
        {
            failures.Add($"Resource indicator \"bad-{audience}\" of \"{audience}\" is not an absolute URI without a fragment.");
            failures.Add($"Scope \"{scope}\" of \"{audience}\" is empty, contains a space or is a protocol scope.");
            failures.Add($"A tenant of \"{audience}\" named \"fixture\" has no id.");
        }
        failures.AddRange(["A resource server has no audience.", "Resource server \"\" has no resource indicators.", "Resource server \"\" has no scopes."]);
        foreach (var audience in new[] { "b", "a", "b", "a" })
        {
            if (!development) failures.Add($"Resource server \"{audience}\": configured tenants are development fixtures.");
            failures.Add($"Resource server \"{audience}\" has both configured tenants and a tenants callback. Configure one of them.");
            if (audience == "b") failures.Add($"The tenants callback URL of \"{audience}\" must be an absolute HTTP(S) URI without user info or a fragment.");
            else if (!development) failures.Add($"The tenants callback of \"{audience}\" requires HTTPS outside Development.");
            failures.Add($"The tenants callback timeout of \"{audience}\" must be greater than zero and at most ten seconds.");
            if (!development) failures.Add($"The tenants callback secret of \"{audience}\" must have at least 32 characters outside Development.");
        }
        if (!development) failures.Add("Resource server \"\" needs a tenants callback outside Development.");
        foreach (var client in new[] { "b", "a", "b", "a" }) failures.Add($"Introspection client \"{client}\" needs both a client id and a client secret.");
        if (!development) failures.Add("Identity:DPoP requires an absolute public HTTPS token endpoint without user information, query or fragment (Development permits loopback HTTP).");
        failures.AddRange([
            "Identity:DPoP required clients must be registered token clients.",
            "Identity:DPoP requires the fixed 300-second proof lifetime, 30-second future skew and 390-second replay TTL.",
            "Identity:Device user codes require 8–32 characters from at least twenty distinct uppercase ASCII letters or digits.",
            "The device code lifetime must be between one and fifteen minutes.",
            "The device polling interval must be a whole number of seconds between five and sixty.",
            "Device verification needs a one-to-sixty-minute window and bounded positive code, account, IP, global and issuance limits.",
            "The token-exchange issuer \"\" must be an absolute HTTPS URI without user info or a fragment.",
            "The JWKS URI of token-exchange issuer \"\" must be an absolute HTTP(S) URI without user info or a fragment.",
            "Token-exchange issuer \"\" needs at least one non-empty audience.",
            "Token-exchange client \"\" needs both a client id and a client secret.",
            "Token-exchange client \"\" must name at least one issuer.",
            "Token-exchange client \"\" must name at least one resource.",
            "TokenRateLimit:Window must be positive and no greater than one hour.",
            "TokenRateLimit:PerSourceRequests must be positive.",
            "Account read-model catch-up timeout must be positive.",
            "Account lookup revision and link hold must be valid.",
            "Account lookup key must be base64 with at least 256 bits of key material."
        ]);
        return failures;
    }

    static ResourceServerOptions Resource(string audience, string scope, string callback) => new()
    {
        Audience = audience,
        Indicators = [$"bad-{audience}"],
        Scopes = [new() { Name = scope }],
        Tenants = [new() { Name = "fixture" }],
        TenantsCallback = new() { Url = callback, Timeout = TimeSpan.Zero, Secret = "short" }
    };
}

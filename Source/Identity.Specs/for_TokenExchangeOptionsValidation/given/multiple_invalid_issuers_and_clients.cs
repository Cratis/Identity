// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;

namespace Cratis.Identity.for_TokenExchangeOptionsValidation.given;

public class multiple_invalid_issuers_and_clients : Specification
{
    protected IdentityOptions options;
    protected List<string> failures;

    void Establish()
    {
        options = new IdentityOptions
        {
            Issuer = "http://b.example/",
            Resources = [new() { Audience = "known", Scopes = [new() { Name = "inside" }] }],
            IntrospectionClients = [new() { ClientId = "cratis-cli" }, new() { ClientId = "a" }],
            TokenExchange = new()
            {
                Issuers = [Issuer("b"), Issuer("a"), Issuer("b"), Issuer("a")],
                Clients = [Client("cratis-cli"), Client("a"), Client("cratis-cli"), Client("a")]
            }
        };
        failures = [];
    }

    protected void Validate(bool development) => failures.AddRange(TokenExchangeOptionsValidation.Validate(options, development));

    protected static IEnumerable<string> Expected(bool development)
    {
        var expected = new List<string>
        {
            "The token-exchange issuer \"http://b.example/\" is configured more than once.",
            "The token-exchange issuer \"http://a.example/\" is configured more than once.",
            "The token-exchange client \"cratis-cli\" is configured more than once.",
            "The token-exchange client \"a\" is configured more than once.",
            "The token-exchange client \"cratis-cli\" collides with another client.",
            "The token-exchange client \"a\" collides with another client.",
            "The token-exchange client \"cratis-cli\" collides with another client.",
            "The token-exchange client \"a\" collides with another client."
        };
        foreach (var name in new[] { "b", "a", "b", "a" })
        {
            var issuer = $"http://{name}.example/";
            expected.Add($"The token-exchange issuer \"{issuer}\" must be an absolute HTTPS URI without user info or a fragment.");
            if (name == "b")
            {
                expected.Add($"The token-exchange issuer \"{issuer}\" must not be Identity's own issuer.");
                expected.Add($"The JWKS URI of token-exchange issuer \"{issuer}\" must be an absolute HTTP(S) URI without user info or a fragment.");
            }
            else if (!development)
            {
                expected.Add($"The JWKS URI of token-exchange issuer \"{issuer}\" requires HTTPS outside Development.");
            }
            expected.Add($"Token-exchange issuer \"{issuer}\" needs at least one non-empty audience.");
            expected.Add($"Token-exchange issuer \"{issuer}\" accepts only asymmetric signing algorithms (RS*, PS*, ES*); none and HMAC algorithms are refused.");
            expected.Add($"The clock skew of token-exchange issuer \"{issuer}\" must be between zero and two minutes.");
            expected.Add($"The maximum token age of token-exchange issuer \"{issuer}\" must be greater than zero and at most one hour.");
            expected.Add($"The JWKS timeout of token-exchange issuer \"{issuer}\" must be greater than zero and at most ten seconds.");
        }
        foreach (var client in new[] { "cratis-cli", "a", "cratis-cli", "a" })
        {
            if (client == "cratis-cli")
            {
                expected.Add($"Token-exchange client \"{client}\" needs both a client id and a client secret.");
            }
            else if (!development)
            {
                expected.Add($"The secret of token-exchange client \"{client}\" must have at least 32 characters outside Development.");
            }
            expected.Add($"Token-exchange client \"{client}\" references an unknown issuer \"unknown\".");
            expected.Add($"Token-exchange client \"{client}\" references a resource \"missing\" that is not configured or was disabled by hosted configuration.");
            expected.Add($"Token-exchange client \"{client}\" names scope \"outside\", which none of its resources own.");
        }
        return expected;
    }

    static ExternalIssuerOptions Issuer(string name) => new()
    {
        Issuer = $"http://{name}.example/",
        JwksUri = name == "b" ? "ftp://b.example/keys" : "http://a.example/keys?query=1",
        Audiences = [""],
        SigningAlgorithms = ["nOnE", "hs256"],
        ClockSkew = TimeSpan.FromSeconds(-1),
        MaxTokenAge = TimeSpan.Zero,
        JwksTimeout = TimeSpan.Zero
    };

    static TokenExchangeClientOptions Client(string id) => new()
    {
        ClientId = id,
        ClientSecret = id == "a" ? "short" : "",
        Issuers = ["unknown"],
        Resources = ["missing", "known"],
        Scopes = ["outside"]
    };
}

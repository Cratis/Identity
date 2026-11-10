// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cratis.Identity.Authorization;
using Cratis.Identity.Revocation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.for_ArcMcpVectors.given;

public class an_authorization_server : for_TokenRateLimitMiddleware.given.an_identity_host
{
    protected const string Verifier = "synthetic-verifier-with-at-least-forty-three-characters";
    static readonly string _challenge = Convert.ToBase64String(SHA256.HashData(Encoding.ASCII.GetBytes(Verifier)))
        .TrimEnd('=').Replace('+', '-').Replace('/', '_');
    protected JsonDocument Vectors;
    protected JsonDocument Manifest;
    protected ServiceProvider Provider;
    private protected IGrantPolicy Policy;
    protected string Resource;
    protected string Audience;
    protected virtual string VectorVersion => "v2.0.0";

    void Establish()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "Vectors");
        var directory = VectorVersion == "v2.0.0" ? Path.Combine(root, "arc-mcp-authorization-v1") : Path.Combine(root, VectorVersion);
        var file = VectorVersion == "v2.0.0" ? "profiles-and-discovery.json" : "arc-mcp-authorization-v1/profiles-and-discovery.json";
        Vectors = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, file)));
        Manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "manifest.json")));
        Resource = Vectors.RootElement.GetProperty("given").GetProperty("fixtureDefaults").GetProperty("resource").GetString();
        Audience = Case("authorization-resource-inferred").GetProperty("then").GetProperty("audience").GetString();
        Builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Identity:Resources:0:Audience"] = Audience,
            ["Identity:Resources:0:Indicators:0"] = Resource,
            ["Identity:Resources:0:Scopes:0:Name"] = "direct:read",
            ["Identity:Resources:1:Audience"] = "studio-api",
            ["Identity:Resources:1:Indicators:0"] = "https://studio.example.test/mcp",
            ["Identity:Resources:1:Scopes:0:Name"] = "studio:read"
        });
        RegisterIdentity();
        Builder.Services.AddOpenIddict().AddServer(server =>
        {
            // Exercise production resource/PKCE options; persistence and client authentication are transport fixtures.
            // Unused endpoint validators satisfy degraded-mode startup without altering their protocol options.
            server.EnableDegradedMode()
                .DisableAuthorizationStorage()
                .DisableTokenStorage()
                .AddEventHandler<ValidateIntrospectionRequestContext>(handler => handler.UseInlineHandler(_ => default))
                .AddEventHandler<ValidateRevocationRequestContext>(handler => handler.UseInlineHandler(_ => default))
                .AddEventHandler<ValidateDeviceAuthorizationRequestContext>(handler => handler.UseInlineHandler(_ => default))
                .AddEventHandler<ValidateEndUserVerificationRequestContext>(handler => handler.UseInlineHandler(_ => default))
                .AddEventHandler<ValidateAuthorizationRequestContext>(handler => handler.UseInlineHandler(_ => default))
                .AddEventHandler<ValidateTokenRequestContext>(handler => handler.UseInlineHandler(_ => default))
                .AddEventHandler<ProcessAuthenticationContext>(handler => handler
                    .SetOrder(OpenIddictServerHandlers.ValidateAuthorizationCode.Descriptor.Order - 1)
                    .UseInlineHandler(context =>
                    {
                        if (context.Transaction.GetProperty<ClaimsPrincipal>(nameof(ProcessAuthenticationContext.AuthorizationCodePrincipal)) is { } principal)
                        {
                            context.AuthorizationCodePrincipal = principal;
                            context.ValidateAuthorizationCode = false;
                        }

                        return default;
                    }))
                .AddEventHandler<ApplyTokenResponseContext>(handler => handler.SetOrder(int.MaxValue).UseInlineHandler(context =>
                {
                    context.HandleRequest();
                    return default;
                }));
        });
        var recovery = Substitute.For<IRecoveryLog>();
        recovery.RecoveryEpoch(Arg.Any<CancellationToken>()).Returns(0L);
        Builder.Services.AddSingleton(recovery);
        ConfigureServices();
        Provider = Builder.Services.BuildServiceProvider();
        Provider.GetRequiredService<RecoveryGate>().Open(true);
        Policy = Provider.GetRequiredService<IGrantPolicy>();
    }

    protected virtual void ConfigureServices()
    {
    }

    protected JsonElement Case(string id)
    {
        int index;
        if (Manifest.RootElement.TryGetProperty("caseIds", out var files))
        {
            var ids = files.GetProperty("arc-mcp-authorization-v1/profiles-and-discovery.json").EnumerateArray().Select(_ => _.GetString()).ToArray();
            index = Array.IndexOf(ids, id);
            index.ShouldBeGreaterThanOrEqual(0);
        }
        else
        {
            var entry = Manifest.RootElement.GetProperty("cases").EnumerateArray().Single(_ => _.GetProperty("id").GetString() == id);
            entry.GetProperty("file").GetString().ShouldEqual("profiles-and-discovery.json");
            index = entry.GetProperty("index").GetInt32();
        }

        return Vectors.RootElement.GetProperty("given").GetProperty("vectors")[index];
    }

    protected OpenIddictServerTransaction Transaction(OpenIddictRequest request, OpenIddictServerEndpointType endpoint) => new()
    {
        Logger = NullLogger.Instance,
        Options = Provider.GetRequiredService<IOptionsMonitor<OpenIddictServerOptions>>().CurrentValue,
        Request = request,
        Response = new OpenIddictResponse(),
        EndpointType = endpoint,
        BaseUri = new Uri("https://localhost/"),
        RequestUri = new Uri(endpoint == OpenIddictServerEndpointType.Authorization ? "https://localhost/connect/authorize" : "https://localhost/connect/token")
    };

    private protected async Task<NotGrantable?> Authorize(OpenIddictParameter resource, string? method, bool pkce, string scope = "direct:read")
    {
        var request = new OpenIddictRequest
        {
            ClientId = "cratis-cli",
            RedirectUri = "http://127.0.0.1/callback",
            ResponseType = ResponseTypes.Code,
            Scope = scope,
            CodeChallenge = pkce ? _challenge : null,
            CodeChallengeMethod = pkce ? method : null
        };
        if (resource != default) request.AddParameter(Parameters.Resource, resource);
        var context = new ValidateAuthorizationRequestContext(Transaction(request, OpenIddictServerEndpointType.Authorization));
        await Provider.GetRequiredService<IOpenIddictServerDispatcher>().DispatchAsync(context);
        return context.IsRejected
            ? new NotGrantable(context.Error, context.ErrorDescription)
            : Policy.Evaluate(new GrantRequest(request.GetScopes(), request.GetResources())) as NotGrantable;
    }

    private protected async Task<NotGrantable?> Redeem(string authorizedResource, OpenIddictParameter requestedResource, string? verifier = Verifier)
    {
        var grant = (Grantable)Policy.Evaluate(new GrantRequest(["direct:read"], [authorizedResource]));
        var principal = new ClaimsPrincipal(new ClaimsIdentity("spec"));
        principal.SetResources(grant.Grant.Resource.Audience);
        principal.SetPresenters("cratis-cli");
        principal.SetClaim(Claims.Private.CodeChallenge, _challenge);
        principal.SetClaim(Claims.Private.CodeChallengeMethod, CodeChallengeMethods.Sha256);
        var request = new OpenIddictRequest { GrantType = GrantTypes.AuthorizationCode, ClientId = "cratis-cli", Code = "synthetic-code", CodeVerifier = verifier };
        if (requestedResource != default) request.AddParameter(Parameters.Resource, requestedResource);
        var transaction = Transaction(request, OpenIddictServerEndpointType.Token);

        // Supply code authentication at its actual dispatch boundary: OpenIddict creates a fresh authentication context.
        // Code storage/signature is the transport fixture; resource and verifier validation remain real.
        transaction.SetProperty(nameof(ProcessAuthenticationContext.AuthorizationCodePrincipal), principal);
        var context = new ValidateTokenRequestContext(transaction);
        await Provider.GetRequiredService<IOpenIddictServerDispatcher>().DispatchAsync(context);
        return context.IsRejected
            ? new NotGrantable(context.Error, context.ErrorDescription)
            : Policy.EvaluateTokenRequest(request.GetResources(), principal.GetResources());
    }

    private protected async Task<NotGrantable?> ValidateOtherGrant(string grantType)
    {
        var request = new OpenIddictRequest { GrantType = grantType, ClientId = "cratis-cli" };
        var context = new ValidateTokenRequestContext(Transaction(request, OpenIddictServerEndpointType.Token));
        await Provider.GetRequiredService<IOpenIddictServerDispatcher>().DispatchAsync(context);
        return context.IsRejected ? new NotGrantable(context.Error, context.ErrorDescription) : null;
    }

    void Destroy()
    {
        Provider?.Dispose();
        Vectors?.Dispose();
        Manifest?.Dispose();
    }
}

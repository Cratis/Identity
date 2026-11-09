// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Identity.Authorization.TokenExchange;
using Cratis.Identity.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenIddict.Server;

namespace Cratis.Identity.for_IdentityServiceCollectionExtensions;

public class when_configuring_token_exchange : Specification
{
    ServiceProvider _provider;
    OpenIddictServerOptions _options;
    OpenIddictServerEvents.HandleConfigurationRequestContext _discovery;

    void Establish()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Identity:Issuer"] = "https://identity.example/",
            ["Identity:Resources:0:Audience"] = "direct-api",
            ["Identity:Resources:0:Indicators:0"] = "https://cratis.direct/mcp",
            ["Identity:Resources:0:Scopes:0:Name"] = "direct:mcp:read",
            ["Identity:TokenExchange:Issuers:0:Issuer"] = "https://github.example/",
            ["Identity:TokenExchange:Issuers:0:JwksUri"] = "https://github.example/.well-known/jwks.json",
            ["Identity:TokenExchange:Issuers:0:Audiences:0"] = "exchange-specs",
            ["Identity:TokenExchange:Clients:0:ClientId"] = "exchange-specs",
            ["Identity:TokenExchange:Clients:0:ClientSecret"] = "test-only-exchange-client-secret",
            ["Identity:TokenExchange:Clients:0:Issuers:0"] = "https://github.example/",
            ["Identity:TokenExchange:Clients:0:Resources:0"] = "direct-api",
            ["Identity:TokenExchange:Clients:0:Scopes:0"] = "direct:mcp:read"
        }).Build();
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(Environments.Development);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(environment);
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton(Substitute.For<IChronicleClient>());
        services.AddCratisIdentity(configuration, environment);
        _provider = services.BuildServiceProvider();
    }

    async Task Because()
    {
        _options = _provider.GetRequiredService<IOptionsMonitor<OpenIddictServerOptions>>().CurrentValue;
        _discovery = new(new OpenIddictServerTransaction { Options = _options });
        await new OpenIddictServerHandlers.Discovery.AttachGrantTypes().HandleAsync(_discovery);
    }

    [Fact] void should_enable_the_exchange_grant() => _options.GrantTypes.ShouldContain(TokenExchangeConstants.GrantType);
    [Fact] void should_advertise_the_exchange_grant_in_discovery() => _discovery.GrantTypes.ShouldContain(TokenExchangeConstants.GrantType);
    [Fact] void should_accept_only_the_supported_subject_token_types() => _options.SubjectTokenTypes.ShouldContainOnly(TokenExchangeConstants.JwtTokenType, TokenExchangeConstants.AccessTokenType);
    [Fact] void should_issue_only_access_tokens() => _options.RequestedTokenTypes.ShouldContainOnly(TokenExchangeConstants.AccessTokenType);
    [Fact] void should_default_to_issuing_access_tokens() => _options.DefaultRequestedTokenType.ShouldEqual(TokenExchangeConstants.AccessTokenType);
    [Fact] void should_not_accept_actor_tokens() => _options.ActorTokenTypes.ShouldBeEmpty();
    [Fact] void should_leave_subject_token_validation_to_identity() => _options.Handlers.ShouldNotContain(OpenIddictServerHandlers.ValidateSubjectToken.Descriptor);

    void Destroy() => _provider?.Dispose();
}

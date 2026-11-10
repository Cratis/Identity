// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Accounts;
using Cratis.Identity.Authorization;
using Cratis.Identity.Tenants;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.for_ArcMcpVectors;

public class when_enforcing_resource_and_pkce : given.an_authorization_server
{
    readonly Dictionary<string, (bool Actual, bool Expected)> _outcomes = [];
    JsonWebToken _token;
    bool _validAuthorization;
    bool _validRedemption;
    bool _wrongVerifierAccepted;
    bool _missingVerifierAccepted;
    bool _audienceAsAuthorizationResourceAccepted;
    bool _audienceAsTokenResourceAccepted;
    string[] _advertisedChallengeMethods;

    async Task Because()
    {
        await RunAuthorization("authorization-resource-required");
        await RunAuthorization("authorization-requires-registered-resource");
        await RunAuthorization("authorization-rejects-plain-pkce");
        await RunAuthorization("authorization-requires-pkce");
        await RunRedemption("token-resource-required");
        await RunRedemption("token-resource-must-match");
        _validAuthorization = await Authorize(Resource, CodeChallengeMethods.Sha256, true);
        _validRedemption = await Redeem(Resource, Resource);
        _wrongVerifierAccepted = await Redeem(Resource, Resource, "a-different-synthetic-verifier-with-forty-three-characters");
        _missingVerifierAccepted = await Redeem(Resource, Resource, null);
        _audienceAsAuthorizationResourceAccepted = await Authorize("direct-api", CodeChallengeMethods.Sha256, true);
        _audienceAsTokenResourceAccepted = await Redeem(Resource, "direct-api");
        var metadata = new HandleConfigurationRequestContext(Transaction(new OpenIddictRequest(), OpenIddictServerEndpointType.Configuration));
        await Provider.GetRequiredService<IOpenIddictServerDispatcher>().DispatchAsync(metadata);
        _advertisedChallengeMethods = [.. metadata.CodeChallengeMethods];
        _token = await Issue();
    }

    [Fact] void should_refuse_authorization_without_resource() => Outcome("authorization-resource-required");
    [Fact] void should_refuse_an_unregistered_resource() => Outcome("authorization-requires-registered-resource");
    [Fact] void should_refuse_plain_pkce() => Outcome("authorization-rejects-plain-pkce");
    [Fact] void should_refuse_missing_pkce() => Outcome("authorization-requires-pkce");
    [Fact] void should_refuse_redemption_without_resource() => Outcome("token-resource-required");
    [Fact] void should_refuse_a_different_resource_even_with_the_correct_grant_audience() => Outcome("token-resource-must-match");
    [Fact] void should_accept_s256_for_the_registered_resource() => _validAuthorization.ShouldBeTrue();
    [Fact] void should_accept_redemption_for_the_authorized_resource() => _validRedemption.ShouldBeTrue();
    [Fact] void should_validate_the_s256_verifier_at_redemption() => _wrongVerifierAccepted.ShouldBeFalse();
    [Fact] void should_require_the_verifier_at_redemption() => _missingVerifierAccepted.ShouldBeFalse();
    [Fact] void should_not_accept_the_audience_slug_as_an_authorization_resource() => _audienceAsAuthorizationResourceAccepted.ShouldBeFalse();
    [Fact] void should_not_accept_the_audience_slug_as_a_token_resource() => _audienceAsTokenResourceAccepted.ShouldBeFalse();
    [Fact] void should_advertise_only_s256() => _advertisedChallengeMethods.ShouldContainOnly(CodeChallengeMethods.Sha256);
    [Fact] void should_issue_exactly_the_catalog_audience_not_the_resource_url() => _token.Audiences.ShouldContainOnly("direct-api");
    [Fact] void should_issue_an_access_token_not_an_id_token() => _token.Typ.ShouldEqual("at+jwt");

    void Outcome(string id) => _outcomes[id].Actual.ShouldEqual(_outcomes[id].Expected);

    async Task RunAuthorization(string id)
    {
        var vector = Case(id);
        var given = vector.GetProperty("given");
        var when = vector.GetProperty("when");
        var resource = given.TryGetProperty("authorizationRequestResource", out var indicator) ? indicator.GetString() : Resource;
        if (when.TryGetProperty("resource", out indicator)) resource = indicator.GetString();
        var method = when.TryGetProperty("codeChallengeMethod", out var challenge) ? challenge.GetString() : CodeChallengeMethods.Sha256;
        var pkce = !when.TryGetProperty("pkcePresent", out var present) || present.GetBoolean();
        _outcomes[id] = (await Authorize(resource, method, pkce), vector.GetProperty("then").GetProperty("requestAccepted").GetBoolean());
    }

    async Task RunRedemption(string id)
    {
        var vector = Case(id);
        var given = vector.GetProperty("given");
        _outcomes[id] = (
            await Redeem(given.GetProperty("authorizationRequestResource").GetString(), given.GetProperty("tokenRequestResource").GetString()),
            vector.GetProperty("then").GetProperty("requestAccepted").GetBoolean());
    }

    async Task<JsonWebToken> Issue()
    {
        var grant = ((Grantable)Policy.Evaluate(new GrantRequest(["direct:read"], [Resource]))).Grant;
        var account = new Account(new("7d9d5c9e-0000-4000-8000-000000000001"), "Synthetic Member", null, "42", "synthetic-member", AccountStatus.Active, []);
        var identity = AccessTokenIdentity.Create(account, "cratis-cli", grant, new SelectableTenant("synthetic-tenant", "Synthetic Tenant"), DateTimeOffset.UtcNow);
        var transaction = Transaction(new OpenIddictRequest { GrantType = GrantTypes.AuthorizationCode, ClientId = "cratis-cli" }, OpenIddictServerEndpointType.Token);
        var principal = new ClaimsPrincipal(identity);
        transaction.SetProperty(typeof(ProcessAuthenticationContext).FullName, new ProcessAuthenticationContext(transaction) { AuthorizationCodePrincipal = principal });
        var context = new ProcessSignInContext(transaction) { Principal = principal, GenerateAccessToken = true };
        await Provider.GetRequiredService<IOpenIddictServerDispatcher>().DispatchAsync(context);
        context.IsRejected.ShouldBeFalse();
        return new JsonWebToken(context.AccessToken);
    }
}

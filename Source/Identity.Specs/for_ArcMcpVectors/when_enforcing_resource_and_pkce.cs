// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
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
    const string OtherResource = "https://studio.example.test/mcp";
    readonly Dictionary<string, NotGrantable?> _rejections = [];
    JsonWebToken _token;
    JsonWebToken _inferredToken;
    string[] _advertisedChallengeMethods;

    async Task Because()
    {
        await RunAuthorization("authorization-resource-inferred");
        await RunAuthorization("authorization-requires-registered-resource");
        await RunAuthorization("authorization-rejects-plain-pkce");
        await RunAuthorization("authorization-requires-pkce");
        await RunRedemption("token-resource-inherited");
        await RunRedemption("token-resource-must-match");
        _rejections["valid-authorization"] = await Authorize(Resource, CodeChallengeMethods.Sha256, true);
        _rejections["valid-redemption"] = await Redeem(Resource, Resource);
        _rejections["wrong-verifier"] = await Redeem(Resource, Resource, "a-different-synthetic-verifier-with-forty-three-characters");
        _rejections["missing-verifier"] = await Redeem(Resource, Resource, null);
        _rejections["audience-at-authorization"] = await Authorize("direct-api", CodeChallengeMethods.Sha256, true);
        _rejections["audience-at-redemption"] = await Redeem(Resource, "direct-api");
        _rejections["ambiguous-inference"] = await Authorize(default, CodeChallengeMethods.Sha256, true, "direct:read studio:read");
        _rejections["empty-inference"] = await Authorize(default, CodeChallengeMethods.Sha256, true, string.Empty);
        _rejections["empty-authorization"] = await Authorize(string.Empty, CodeChallengeMethods.Sha256, true);
        _rejections["empty-redemption"] = await Redeem(Resource, string.Empty);
        _rejections["multiple-authorization"] = await Authorize(ImmutableArray.Create<string?>(Resource, OtherResource), CodeChallengeMethods.Sha256, true);
        _rejections["multiple-redemption"] = await Redeem(Resource, ImmutableArray.Create<string?>(Resource, OtherResource));
        _rejections["unknown-redemption"] = await Redeem(Resource, "https://unknown.example.test/mcp");
        _rejections["refresh"] = await ValidateOtherGrant(GrantTypes.RefreshToken);
        _rejections["device"] = await ValidateOtherGrant(GrantTypes.DeviceCode);
        _rejections["exchange"] = await ValidateOtherGrant("urn:ietf:params:oauth:grant-type:token-exchange");
        var metadata = new HandleConfigurationRequestContext(Transaction(new OpenIddictRequest(), OpenIddictServerEndpointType.Configuration));
        await Provider.GetRequiredService<IOpenIddictServerDispatcher>().DispatchAsync(metadata);
        _advertisedChallengeMethods = [.. metadata.CodeChallengeMethods];
        _token = await Issue(false);
        _inferredToken = await Issue(true);
    }

    [Fact] void should_infer_authorization_resource_from_unambiguous_scopes() => AcceptedVector("authorization-resource-inferred");
    [Fact] void should_retain_the_authorized_resource_when_redemption_omits_resource() => AcceptedVector("token-resource-inherited");
    [Fact] void should_refuse_an_unregistered_resource() => RejectedVector("authorization-requires-registered-resource", Errors.InvalidTarget);
    [Fact] void should_refuse_plain_pkce() => RejectedVector("authorization-rejects-plain-pkce", Errors.InvalidRequest);
    [Fact] void should_refuse_missing_pkce() => RejectedVector("authorization-requires-pkce", Errors.InvalidRequest);
    [Fact] void should_refuse_a_different_resource_even_with_the_correct_grant_audience() => RejectedVector("token-resource-must-match", Errors.InvalidTarget, "The requested resource is not the resource this grant was authorized for.");
    [Fact] void should_accept_s256_for_the_registered_resource() => _rejections["valid-authorization"].ShouldBeNull();
    [Fact] void should_accept_redemption_for_the_authorized_resource() => _rejections["valid-redemption"].ShouldBeNull();
    [Fact] void should_validate_the_s256_verifier_at_redemption() => Rejected("wrong-verifier", Errors.InvalidGrant);
    [Fact] void should_require_the_verifier_at_redemption() => Rejected("missing-verifier", Errors.InvalidRequest);
    [Fact] void should_not_accept_the_audience_slug_as_an_authorization_resource() => Rejected("audience-at-authorization", Errors.InvalidRequest);
    [Fact] void should_not_accept_the_audience_slug_as_a_token_resource() => Rejected("audience-at-redemption", Errors.InvalidRequest);
    [Fact] void should_reject_ambiguous_scope_inference() => Rejected("ambiguous-inference", Errors.InvalidTarget, "The resource parameter is required to determine which resource server the grant is for.");
    [Fact] void should_reject_empty_scope_inference() => Rejected("empty-inference", Errors.InvalidTarget, "The resource parameter is required to determine which resource server the grant is for.");
    [Fact] void should_reject_an_explicit_empty_authorization_resource() => Rejected("empty-authorization", Errors.InvalidTarget, "The requested resource is not known.");
    [Fact] void should_reject_an_explicit_empty_redemption_resource() => Rejected("empty-redemption", Errors.InvalidTarget, "The requested resource is not known.");
    [Fact] void should_reject_multiple_authorization_resources() => Rejected("multiple-authorization", Errors.InvalidTarget, "A grant can only be bound to one resource server; request each resource separately.");
    [Fact] void should_reject_multiple_redemption_resources() => Rejected("multiple-redemption", Errors.InvalidTarget, "The requested resource is not the resource this grant was authorized for.");
    [Fact] void should_reject_an_unknown_redemption_resource() => Rejected("unknown-redemption", Errors.InvalidTarget);
    [Fact] void should_leave_refresh_token_parameter_validation_unchanged_without_resource() => Rejected("refresh", Errors.InvalidRequest);
    [Fact] void should_leave_disabled_device_grant_validation_unchanged_without_resource() => Rejected("device", Errors.UnsupportedGrantType);
    [Fact] void should_leave_disabled_token_exchange_validation_unchanged_without_resource() => Rejected("exchange", Errors.UnsupportedGrantType);
    [Fact] void should_advertise_only_s256() => _advertisedChallengeMethods.ShouldContainOnly(CodeChallengeMethods.Sha256);
    [Fact] void should_issue_exactly_the_catalog_audience_not_the_resource_url() => _token.Audiences.ShouldContainOnly(Audience);
    [Fact] void should_preserve_the_catalog_audience_when_resource_is_inferred() => _inferredToken.Audiences.ShouldContainOnly(Audience);
    [Fact] void should_issue_an_access_token_not_an_id_token() => _token.Typ.ShouldEqual("at+jwt");

    void AcceptedVector(string id)
    {
        Case(id).GetProperty("then").GetProperty("requestAccepted").GetBoolean().ShouldBeTrue();
        _rejections[id].ShouldBeNull();
    }

    void RejectedVector(string id, string error, string? description = null)
    {
        Case(id).GetProperty("then").GetProperty("requestAccepted").GetBoolean().ShouldBeFalse();
        Rejected(id, error, description);
    }

    void Rejected(string id, string error, string? description = null)
    {
        _rejections[id].ShouldNotBeNull();
        _rejections[id].Error.ShouldEqual(error);

        // OpenIddict's native descriptions are localized; only application-owned descriptions are stable here.
        if (description is not null) _rejections[id].Description.ShouldEqual(description);
    }

    async Task RunAuthorization(string id)
    {
        var vector = Case(id);
        var given = vector.GetProperty("given");
        var when = vector.GetProperty("when");
        var resource = given.TryGetProperty("authorizationRequestResource", out var indicator) ? indicator.GetString() : Resource;
        if (when.TryGetProperty("resource", out indicator)) resource = indicator.GetString();
        var method = when.TryGetProperty("codeChallengeMethod", out var challenge) ? challenge.GetString() : CodeChallengeMethods.Sha256;
        var pkce = !when.TryGetProperty("pkcePresent", out var present) || present.GetBoolean();
        var scope = given.TryGetProperty("authorizationRequestScopes", out var scopes)
            ? string.Join(' ', scopes.EnumerateArray().Select(value => value.GetString()))
            : "direct:read";
        _rejections[id] = await Authorize(resource, method, pkce, scope);
    }

    async Task RunRedemption(string id)
    {
        var given = Case(id).GetProperty("given");
        _rejections[id] = await Redeem(given.GetProperty("authorizationRequestResource").GetString(), given.GetProperty("tokenRequestResource").GetString());
    }

    async Task<JsonWebToken> Issue(bool inferResource)
    {
        var grant = ((Grantable)Policy.Evaluate(new GrantRequest(["direct:read"], inferResource ? [] : [Resource]))).Grant;
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

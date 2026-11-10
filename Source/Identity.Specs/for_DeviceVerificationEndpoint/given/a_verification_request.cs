// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using Cratis.Identity.Accounts;
using Cratis.Identity.Authorization;
using Cratis.Identity.Configuration;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Device;
using Cratis.Identity.given;
using Cratis.Identity.Revocation;
using Cratis.Identity.Tenants;
using Cratis.Identity.Upstream;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;

namespace Cratis.Identity.for_DeviceVerificationEndpoint.given;

public class a_verification_request : Specification
{
    protected const string Code = "BCDFGHJK";
    private protected IAuthenticationService _authentication;
    private protected IAntiforgery _antiforgery;
    private protected IGrantPolicy _policy;
    private protected ITenantDirectory _tenants;
    private protected IOpenIddictApplicationManager _applications;
    private protected IOpenIddictTokenManager _tokens;
    private protected IIndependentFenceLog _log;
    private protected IAccountReader _accounts;
    private protected IdentityOptions _options;
    private protected Account _account;
    private protected Grant _grant;
    protected ClaimsPrincipal _principal;
    protected object _client;
    protected string? _action;
    protected string _tenant;
    protected string? _proof;
    protected string _method;
    protected string? _code;
    protected string? _cookie;
    protected string? _done;
    DeviceGrantAdmission _admission;
    DeviceGrantApproval _approval;

    void Establish()
    {
        _authentication = Substitute.For<IAuthenticationService>();
        _antiforgery = Substitute.For<IAntiforgery>();
        _policy = Substitute.For<IGrantPolicy>();
        _tenants = Substitute.For<ITenantDirectory>();
        _applications = Substitute.For<IOpenIddictApplicationManager>();
        _tokens = Substitute.For<IOpenIddictTokenManager>();
        _log = Substitute.For<IIndependentFenceLog>();
        _accounts = Substitute.For<IAccountReader>();
        _options = new();
        _options.DeviceGrant.Enabled = true;
        _account = new(new("7d9d5c9e-0000-4000-8000-000000000001"), "Member", null, "42", "member", AccountStatus.Active, []);
        _grant = new(new Resources.ResourceCatalog(Configurations.TwoResourceServers().AsOptions()).FindByAudience(Configurations.DirectAudience), ["direct:read"]);
        _principal = new ClaimsPrincipal(new ClaimsIdentity("test")).SetTokenId("user-token")
            .SetClaim(OpenIddictConstants.Claims.Private.DeviceCodeId, "device-token")
            .SetClaim(OpenIddictConstants.Claims.ClientId, "device-client")
            .SetClaim(DeviceAuthorizationEndpoint.ResourceClaim, _grant.Resource.Indicators.First())
            .SetScopes(_grant.Scopes).SetExpirationDate(DateTimeOffset.UtcNow.AddMinutes(10));
        _client = new();
        _action = "review";
        _tenant = "tenant";
        _method = HttpMethods.Post;
        _code = Code;
        _cookie = "browser-session";
        ConfigureAuthentication();
        ConfigureAdmission();
        ConfigureGrant();
        _approval = new(new EphemeralDataProtectionProvider());
    }

    void ConfigureAuthentication()
    {
        _authentication.AuthenticateAsync(Arg.Any<HttpContext>(), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)
            .Returns(_ => AuthenticateResult.Success(new(_principal, "test")));
        _authentication.AuthenticateAsync(Arg.Any<HttpContext>(), IdentitySchemes.Session)
            .Returns(AuthenticateResult.Success(new(SessionPrincipal.For(_account, "github", DateTimeOffset.FromUnixTimeSeconds(1_700_000_000)), "test")));
        _accounts.FindActive(Arg.Any<AccountSubject>(), Arg.Any<CancellationToken>()).Returns(_account);
        _antiforgery.IsRequestValidAsync(Arg.Any<HttpContext>()).Returns(true);
        _antiforgery.GetAndStoreTokens(Arg.Any<HttpContext>()).Returns(new AntiforgeryTokenSet("request", "cookie", "csrf", "header"));
    }

    void ConfigureAdmission()
    {
        var gate = new RecoveryGate();
        gate.ObserveEpoch(7);
        gate.Open(true);
        _log.RecoveryEpoch(Arg.Any<CancellationToken>()).Returns(7L);
        _tokens.FindByIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(new object());
        _tokens.GetExpirationDateAsync(Arg.Any<object>(), Arg.Any<CancellationToken>()).Returns(DateTimeOffset.UtcNow.AddMinutes(10));
        _tokens.GetPropertiesAsync(Arg.Any<object>(), Arg.Any<CancellationToken>()).Returns(ImmutableDictionary<string, JsonElement>.Empty.Add(RecoveryEpochStamp.PropertyName, JsonSerializer.SerializeToElement(7)));
        _admission = new(gate, _log, _tokens, Substitute.For<IOpenIddictAuthorizationManager>(), Substitute.For<IFenceStore>(), _accounts);
    }

    void ConfigureGrant()
    {
        _policy.Evaluate(Arg.Any<GrantRequest>()).Returns(new Grantable(_grant));
        _tenants.SelectableTenants(Arg.Any<Account>(), Arg.Any<Resources.ResourceServer>(), Arg.Any<CancellationToken>()).Returns(new TenantsListed([new("tenant", "Tenant")]));
        _applications.FindByClientIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(_client);
        _applications.GetDisplayNameAsync(_client, Arg.Any<CancellationToken>()).Returns("Device client");
    }

    protected async Task<IResult> Submit()
    {
        await using var services = new ServiceCollection().AddSingleton(_authentication).BuildServiceProvider();
        var http = new DefaultHttpContext { RequestServices = services };
        http.Request.Method = _method;
        http.Request.PathBase = "/id";
        http.Request.Host = new("issuer.example");
        http.Request.Path = "/device";
        if (_cookie is not null) http.Request.Headers.Cookie = $"cratis-identity-session={_cookie}";
        http.Features.Set(new OpenIddictServerAspNetCoreFeature { Transaction = new OpenIddictServerTransaction { Request = new OpenIddictRequest(), Logger = NullLogger.Instance, Options = new OpenIddictServerOptions() } });
        http.Request.ContentType = "application/x-www-form-urlencoded";
        http.Request.Form = new FormCollection(new()
        {
            ["user_code"] = _code,
            ["action"] = _action,
            ["tenant"] = _tenant,
            ["proof"] = _proof
        });
        if (_done is not null) http.Request.QueryString = QueryString.Create("done", _done);
        var upstream = Options.Create(new UpstreamLoginOptions());
        var endpoint = new DeviceVerificationEndpoint(new(_accounts, upstream), _antiforgery, _applications, _tokens, _policy, _admission, _approval, _tenants, upstream, Options.Create(_options), TimeProvider.System);
        var handle = typeof(DeviceVerificationEndpoint).GetMethod("Handle", BindingFlags.Instance | BindingFlags.NonPublic);
        return await (Task<IResult>)handle.Invoke(endpoint, [http]);
    }

    protected async Task CaptureProof()
    {
        var review = (Microsoft.AspNetCore.Http.HttpResults.ContentHttpResult)await Submit();
        const string marker = "name=\"proof\" value=\"";
        var start = review.ResponseContent.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
        _proof = System.Net.WebUtility.HtmlDecode(review.ResponseContent[start..review.ResponseContent.IndexOf('"', start)]);
    }
}

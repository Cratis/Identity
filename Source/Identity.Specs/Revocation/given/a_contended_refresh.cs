// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts;
using Cratis.Identity.Authorization;
using Cratis.Identity.Contracts;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Resources;
using Cratis.Identity.Revocation;
using Cratis.Identity.Tenants;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.RevocationSpecs.given;

public class a_contended_refresh : for_RedeemRefreshTokenOnce.given.a_refresh_token_request
{
    private protected SharedFenceStore _authority;
    private protected GrantRevocation _grants;
    private protected ReadRefreshGrant _read;
    private protected RefreshIssuance _loserIssuance;
    protected ProcessSignInContext _loser;
    protected ValidateTokenContext _validation;
    private protected GrantState _final;
    protected readonly TaskCompletionSource _consumed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);

    async Task Establish()
    {
        _authority = new SharedFenceStore();
        await _authority.RecordFamilyStart(AuthorizationId, TokenId, FenceDoubles.Subject, _time.GetUtcNow(), _time.GetUtcNow().AddDays(30), CancellationToken.None);
        _grants = new(_tokens, _authorizations, _authority, FenceDoubles.Confirmation(_authority));
        _read = new(_authority, _tokens, _authorizations, _time);
        _handler = new(_tokens, _grants, _time, _authority, _read, _issuance);
        _tokens.HasStatusAsync(_token, Statuses.Valid, Arg.Any<CancellationToken>()).Returns(true);
        _tokens.TryRedeemAsync(_token, Arg.Any<CancellationToken>()).Returns(_ => new ValueTask<bool>(CompleteRedemption()));
        _loserIssuance = new();
        _loser = new ProcessSignInContext(_context.Transaction);
        var principal = _context.Transaction.GetProperty<ProcessAuthenticationContext>(typeof(ProcessAuthenticationContext).FullName).RefreshTokenPrincipal;
        _validation = new ValidateTokenContext(_context.Transaction)
        {
            TokenId = TokenId,
            AuthorizationId = AuthorizationId,
            Principal = principal.Clone().SetTokenType(TokenTypeIdentifiers.RefreshToken)
        };
    }

    protected async Task<IResult> ReachTokenEndpoint()
    {
        var account = new Account(new(FenceDoubles.Subject), "Member", null, "42", "member", AccountStatus.Active, []);
        var principal = _validation.Principal.Clone()
            .SetClaim(Claims.Subject, FenceDoubles.Subject)
            .SetClaim(IdentityClaims.TenantId, "tenant")
            .SetResources("audience");
        var accounts = Substitute.For<IAccountReader>();
        accounts.FindActive(Arg.Any<AccountSubject>(), Arg.Any<CancellationToken>()).Returns(account);
        var resource = new ResourceServer("audience", "Resource", [], []);
        var resources = Substitute.For<IResourceCatalog>();
        resources.FindByAudience("audience").Returns(resource);
        var tenants = Substitute.For<ITenantDirectory>();
        tenants.SelectableTenants(account, resource, Arg.Any<CancellationToken>()).Returns(new TenantsListed([new("tenant", "Tenant")]));
        var authentication = Substitute.For<IAuthenticationService>();
        authentication.AuthenticateAsync(Arg.Any<HttpContext>(), Arg.Any<string>()).Returns(AuthenticateResult.Success(new(principal, "test")));
        await using var services = new ServiceCollection().AddSingleton(authentication).BuildServiceProvider();
        var http = new DefaultHttpContext { RequestServices = services };
        http.Features.Set(new OpenIddictServerAspNetCoreFeature { Transaction = _context.Transaction });
        var endpoint = new TokenEndpoint(accounts, Substitute.For<IGrantPolicy>(), resources, tenants, NullLogger<TokenEndpoint>.Instance, _authority, new Device.DeviceGrantAdmission(new RecoveryGate(), Substitute.For<IIndependentFenceLog>(), _tokens, _authorizations, _authority, accounts));
        return await endpoint.Handle(http);
    }

    async Task<bool> CompleteRedemption()
    {
        _consumed.TrySetResult();
        await _release.Task.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
        return true;
    }

    private protected RedeemRefreshTokenOnce LosingHandler() => new(_tokens, _grants, _time, _authority, _read, _loserIssuance);

    private protected RejectFencedGrants LosingGuard() => new(_authority, _tokens, _read, _grants, _loserIssuance);
}

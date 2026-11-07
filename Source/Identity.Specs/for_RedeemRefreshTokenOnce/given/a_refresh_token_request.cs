// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Authorization;
using Cratis.Identity.given;
using Cratis.Identity.Revocation;
using Cratis.Identity.RevocationSpecs;
using Microsoft.Extensions.Logging.Abstractions;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.for_RedeemRefreshTokenOnce.given;

public class a_refresh_token_request : Specification
{
    protected const string TokenId = "token-1";
    protected const string AuthorizationId = "authorization-1";

    protected IOpenIddictTokenManager _tokens;
    protected IOpenIddictAuthorizationManager _authorizations;
    private protected RedeemRefreshTokenOnce _handler;
    private protected IFenceStore _fences;
    private protected RefreshIssuance _issuance;
    protected FixedTimeProvider _time;
    protected ProcessSignInContext _context;
    protected object _token;
    protected object _authorization;

    void Establish()
    {
        _tokens = Substitute.For<IOpenIddictTokenManager>();
        _authorizations = Substitute.For<IOpenIddictAuthorizationManager>();
        _time = new FixedTimeProvider(KeyMaterial.Now);
        _fences = FenceDoubles.Store(AuthorizationId, TokenId, _time.GetUtcNow());
        _issuance = new RefreshIssuance();
        var grants = new GrantRevocation(_tokens, _authorizations, _fences, FenceDoubles.Confirmation(_fences));
        _handler = new RedeemRefreshTokenOnce(_tokens, grants, _time, _fences, new ReadRefreshGrant(_fences, _tokens, _authorizations, _time), _issuance);
        _token = new object();
        _authorization = new object();

        _tokens.FindByIdAsync(TokenId, Arg.Any<CancellationToken>()).Returns(_token);
        _tokens.GetExpirationDateAsync(_token, Arg.Any<CancellationToken>()).Returns(KeyMaterial.Now.AddDays(1));
        _authorizations.FindByIdAsync(AuthorizationId, Arg.Any<CancellationToken>()).Returns(_authorization);
        _authorizations.HasStatusAsync(_authorization, Statuses.Revoked, Arg.Any<CancellationToken>()).Returns(false);
        _authorizations.TryRevokeAsync(_authorization, Arg.Any<CancellationToken>()).Returns(true);

        var transaction = new OpenIddictServerTransaction
        {
            EndpointType = OpenIddictServerEndpointType.Token,
            Logger = NullLogger.Instance,
            Options = new OpenIddictServerOptions(),
            Request = new OpenIddictRequest { GrantType = GrantTypes.RefreshToken }
        };

        var principal = new ClaimsPrincipal(new ClaimsIdentity("test"))
            .SetTokenId(TokenId)
            .SetAuthorizationId(AuthorizationId);
        transaction.SetProperty(typeof(ProcessAuthenticationContext).FullName, new ProcessAuthenticationContext(transaction) { RefreshTokenPrincipal = principal });
        _context = new ProcessSignInContext(transaction);
    }
}

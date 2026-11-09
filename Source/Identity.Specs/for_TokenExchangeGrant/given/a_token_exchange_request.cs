// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Accounts;
using Cratis.Identity.Accounts.Lookup;
using Cratis.Identity.Authorization.TokenExchange;
using Cratis.Identity.Configuration;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.for_ProductTenantsCallback.given;
using Cratis.Identity.given;
using Cratis.Identity.Resources;
using Cratis.Identity.Tenants;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.for_TokenExchangeGrant.given;

public class a_token_exchange_request : for_AccessTokenIdentity.given.an_account_and_a_grant
{
    private protected IdentityOptions _options;
    private protected FixedTimeProvider _time;
    private protected ISubjectTokenValidator _validator;
    private protected SubjectTokenReplayGuard _replay;
    private protected ISubjectTokenReplayGuard _replayGuard;
    private protected IUpstreamKeyResolver _keys;
    private protected IAccountReader _accounts;
    private protected ITenantDirectory _tenants;
    private protected TokenExchangeGrant _handler;
    private protected OpenIddictRequest _request;
    private protected IResult _result;
    private protected ValidatedSubject _subject;
    private protected CallbackLogger<TokenExchangeGrant> _logger;

    void Establish()
    {
        _options = Configurations.WithTokenExchange();
        _time = new(DateTimeOffset.UtcNow);
        _replay = new(_time);
        _replayGuard = _replay;
        _logger = new();
        _subject = new(new UpstreamIssuer(Configurations.ExternalIssuer), new UpstreamSubject("external-subject-1"), 1_700_000_000, _time.Now.AddMinutes(5), _time.Now, "key-1");

        _validator = Substitute.For<ISubjectTokenValidator>();
        _validator.Validate(default!, default!, default).ReturnsForAnyArgs(Task.FromResult(SubjectTokenResult.Accepted(_subject)));

        _keys = Substitute.For<IUpstreamKeyResolver>();
        _keys.Find(default!, default!).ReturnsForAnyArgs(Task.FromResult<UpstreamKeyMatch?>(new UpstreamKeyMatch(_account.Id, default!, true)));

        _accounts = Substitute.For<IAccountReader>();
        _accounts.FindActive(default!, default).ReturnsForAnyArgs(Task.FromResult<Account?>(_account));

        _tenants = Substitute.For<ITenantDirectory>();
        _tenants.SelectableTenants(default!, default!, default).ReturnsForAnyArgs(Task.FromResult<TenantLookup>(new TenantsListed([new SelectableTenant("cratis", "Cratis")])));

        _request = NewRequest();
    }

    private protected static OpenIddictRequest NewRequest()
    {
        var request = new OpenIddictRequest
        {
            GrantType = TokenExchangeConstants.GrantType,
            ClientId = Configurations.ExchangeClientId,
            Scope = "direct:read"
        };
        return (OpenIddictRequest)request
            .SetParameter(TokenExchangeConstants.SubjectToken, "subject-token-placeholder")
            .SetParameter(TokenExchangeConstants.SubjectTokenType, TokenExchangeConstants.JwtTokenType)
            .SetParameter(Parameters.Resource, Configurations.DirectMcp)
            .SetParameter(TokenExchangeConstants.Tenant, "cratis");
    }

    private protected static string ErrorOf(IResult result) => ((ForbidHttpResult)result).Properties!.Items[OpenIddictServerAspNetCoreConstants.Properties.Error]!;

    private protected static string DescriptionOf(IResult result) => ((ForbidHttpResult)result).Properties!.Items[OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription]!;

    private protected async Task<IResult> Run(Action<OpenIddictRequest> change)
    {
        _request = NewRequest();
        change(_request);
        await Handle();
        return _result;
    }

    private protected async Task Handle()
    {
        _handler = new(_validator, _replayGuard, _keys, _accounts, new ResourceCatalog(_options.AsOptions()), _tenants, _options.AsOptions(), _time, _logger);
        _result = await _handler.Handle(_request);
    }

    private protected string Error => ((ForbidHttpResult)_result).Properties!.Items[OpenIddictServerAspNetCoreConstants.Properties.Error]!;

    private protected string Description => ((ForbidHttpResult)_result).Properties!.Items[OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription]!;

    private protected ClaimsPrincipal Issued => ((SignInHttpResult)_result).Principal;

    private protected bool IsError => _result is ForbidHttpResult;
}

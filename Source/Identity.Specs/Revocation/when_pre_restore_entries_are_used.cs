// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Authorization;
using Cratis.Identity.Revocation;
using Microsoft.Extensions.Logging.Abstractions;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.RevocationSpecs;

public class when_pre_restore_entries_are_used : given.an_epoch_store
{
    ValidateTokenContext _before;
    ValidateTokenContext _code;
    ValidateTokenContext _refresh;
    ValidateTokenContext _access;
    ValidateTokenContext _authorizationOnly;
    ProcessSignInContext _signIn;

    async Task Because()
    {
        var tokens = Substitute.For<IOpenIddictTokenManager>();
        var authorizations = Substitute.For<IOpenIddictAuthorizationManager>();
        tokens.FindByIdAsync(_token.Id.ToString(), Arg.Any<CancellationToken>()).Returns(_tokenRows.Entry);
        authorizations.FindByIdAsync(_authorization.Id.ToString(), Arg.Any<CancellationToken>()).Returns(_authorizationRows.Entry);
        tokens.GetPropertiesAsync(Arg.Any<object>(), Arg.Any<CancellationToken>()).Returns(_ => _tokens.GetPropertiesAsync(_tokenRows.Entry, CancellationToken.None));
        authorizations.GetPropertiesAsync(Arg.Any<object>(), Arg.Any<CancellationToken>()).Returns(_ => _authorizations.GetPropertiesAsync(_authorizationRows.Entry, CancellationToken.None));
        var log = Substitute.For<IRecoveryLog>();
        log.RecoveryEpoch(Arg.Any<CancellationToken>()).Returns(_ => _gate.Epoch);
        var handler = new RefuseIssuanceDuringRecovery(_gate, log, tokens, authorizations);
        _before = Validation(TokenTypeIdentifiers.Private.AuthorizationCode);
        await handler.HandleAsync(_before);
        _gate.ObserveEpoch(8);
        _gate.Open(true);
        _code = Validation(TokenTypeIdentifiers.Private.AuthorizationCode);
        _refresh = Validation(TokenTypeIdentifiers.RefreshToken);
        _access = Validation(TokenTypeIdentifiers.AccessToken, OpenIddictServerEndpointType.Introspection);
        await handler.HandleAsync(_code);
        await handler.HandleAsync(_refresh);
        await handler.HandleAsync(_access);
        _signIn = new(Transaction(OpenIddictServerEndpointType.Token))
        {
            Principal = new ClaimsPrincipal(new ClaimsIdentity()).SetTokenId(_token.Id.ToString()).SetAuthorizationId(_authorization.Id.ToString())
        };
        await handler.HandleAsync(_signIn);
        _tokenRows.Entry.Properties[RecoveryEpochStamp.PropertyName] = 8L;
        _authorizationOnly = Validation(TokenTypeIdentifiers.Private.AuthorizationCode);
        await handler.HandleAsync(_authorizationOnly);
    }

    ValidateTokenContext Validation(string type, OpenIddictServerEndpointType endpoint = OpenIddictServerEndpointType.Token) => new(Transaction(endpoint))
    {
        TokenId = _token.Id.ToString(),
        AuthorizationId = _authorization.Id.ToString(),
        Principal = new ClaimsPrincipal(new ClaimsIdentity()).SetTokenType(type)
    };

    static OpenIddictServerTransaction Transaction(OpenIddictServerEndpointType endpoint) => new()
    {
        EndpointType = endpoint,
        Logger = NullLogger.Instance,
        Options = new OpenIddictServerOptions()
    };

    [Fact] void should_accept_the_actually_inserted_entries_before_restore() => _before.IsRejected.ShouldBeFalse();
    [Fact] void should_deny_an_old_code_after_the_gate_reopens() => _code.Error.ShouldEqual(Errors.InvalidGrant);
    [Fact] void should_deny_an_old_refresh_entry_after_the_gate_reopens() => _refresh.Error.ShouldEqual(Errors.InvalidGrant);
    [Fact] void should_deny_an_old_access_entry_at_introspection() => _access.Error.ShouldEqual(Errors.InvalidToken);
    [Fact] void should_deny_a_stale_authorization_even_with_a_current_token() => _authorizationOnly.Error.ShouldEqual(Errors.InvalidGrant);
    [Fact] void should_deny_sign_in_from_a_stale_entry() => _signIn.Error.ShouldEqual(Errors.InvalidGrant);
    [Fact]
    void should_run_after_identifiers_and_before_fence_validation() =>
        (RefuseIssuanceDuringRecovery.EntryValidationDescriptor.Order < RejectFencedGrants.Descriptor.Order).ShouldBeTrue();
}

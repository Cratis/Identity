// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;
using Cratis.Identity.Revocation;
using Microsoft.Extensions.Logging.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.RevocationSpecs;

public class when_recovery_is_unproven : Specification
{
    RefuseIssuanceDuringRecovery _handler;
    ProcessRequestContext _authorization;
    ProcessRequestContext _token;
    ProcessRequestContext _introspection;
    ProcessSignInContext _issuance;
    ValidateTokenContext _validation;
    ValidateTokenContext _revocation;

    void Establish()
    {
        _handler = new(new RecoveryGate(), Substitute.For<IRecoveryLog>());
        _authorization = new(Transaction(OpenIddictServerEndpointType.Authorization));
        _token = new(Transaction(OpenIddictServerEndpointType.Token));
        _introspection = new(Transaction(OpenIddictServerEndpointType.Introspection));
        _issuance = new(Transaction(OpenIddictServerEndpointType.Token));
        _validation = new(Transaction(OpenIddictServerEndpointType.Token));
        _revocation = new(Transaction(OpenIddictServerEndpointType.Revocation));
    }

    async Task Because()
    {
        await _handler.HandleAsync(_authorization);
        await _handler.HandleAsync(_token);
        await _handler.HandleAsync(_introspection);
        await _handler.HandleAsync(_issuance);
        await _handler.HandleAsync(_validation);
        await _handler.HandleAsync(_revocation);
    }

    [Fact] void should_deny_authorization_requests() => _authorization.IsRejected.ShouldBeTrue();
    [Fact] void should_deny_every_token_grant() => _token.IsRejected.ShouldBeTrue();
    [Fact] void should_deny_introspection() => _introspection.IsRejected.ShouldBeTrue();
    [Fact] void should_deny_sign_in_issuance() => _issuance.IsRejected.ShouldBeTrue();
    [Fact] void should_deny_server_side_token_use() => _validation.IsRejected.ShouldBeTrue();
    [Fact] void should_leave_revocation_available_to_add_denials() => _revocation.IsRejected.ShouldBeFalse();

    static OpenIddictServerTransaction Transaction(OpenIddictServerEndpointType endpoint) => new()
    {
        EndpointType = endpoint,
        Logger = NullLogger.Instance,
        Options = new OpenIddictServerOptions()
    };
}

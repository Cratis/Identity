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

public class when_rfc7009_revokes_a_refresh_family : Specification
{
    SharedFenceStore _fences;
    RevokeGrantOnRefreshTokenRevocation _handler;
    HandleRevocationRequestContext _context;
    GrantState _state;

    async Task Establish()
    {
        _fences = new SharedFenceStore();
        await _fences.RecordFamilyStart("grant", "entry", FenceDoubles.Subject, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddDays(30), CancellationToken.None);
        var grants = new GrantRevocation(Substitute.For<IOpenIddictTokenManager>(), Substitute.For<IOpenIddictAuthorizationManager>(), _fences, FenceDoubles.Confirmation(_fences));
        _handler = new(grants, NullLogger<RevokeGrantOnRefreshTokenRevocation>.Instance);
        _context = new HandleRevocationRequestContext(new OpenIddictServerTransaction { Logger = NullLogger.Instance, Options = new(), EndpointType = OpenIddictServerEndpointType.Revocation })
        {
            GenericTokenPrincipal = new ClaimsPrincipal(new ClaimsIdentity("test")).SetTokenType(TokenTypeIdentifiers.RefreshToken).SetAuthorizationId("grant")
        };
    }

    async Task Because()
    {
        await _handler.HandleAsync(_context);
        _state = await _fences.Read("grant", CancellationToken.None);
    }
    [Fact] void should_commit_the_family_restriction() => _state.Fence.Reason.ShouldEqual(RevocationReason.RefreshTokenRevoked);
    [Fact] void should_confirm_every_restriction_intent_before_returning() => _state.Outbox.All(entry => entry.State != OutboxState.Pending).ShouldBeTrue();
    [Fact] void should_not_report_explicit_revocation_as_reuse() => _state.Outbox.Any(entry => entry.Kind == FactKind.Reuse).ShouldBeFalse();
    [Fact] void should_commit_before_the_stock_entry_revocation() => (RevokeGrantOnRefreshTokenRevocation.Descriptor.Order < OpenIddictServerHandlers.Revocation.RevokeToken.Descriptor.Order).ShouldBeTrue();
}

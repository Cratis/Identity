// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization.ConnectedApps;
using Cratis.Identity.Revocation;
using Cratis.Identity.RevocationSpecs;
using OpenIddict.Abstractions;

namespace Cratis.Identity.Authorization.for_RevokeConnectedApp.given;

public class a_connected_app : for_ConnectedApp.given.connected_grants
{
    protected IOpenIddictTokenManager _tokens;
    private protected IFenceLog _log;
    private protected ConnectedAppRevocation _revocation;
    protected object _authorization;
    protected Exception _error;

    void Establish()
    {
        _authorization = Grant("grant", DateTimeOffset.UnixEpoch);
        _authorizations.FindByIdAsync("grant", Arg.Any<CancellationToken>()).Returns(_authorization);
        _authorizations.GetSubjectAsync(_authorization, Arg.Any<CancellationToken>()).Returns(Subject);
        _authorizations.TryRevokeAsync(_authorization, Arg.Any<CancellationToken>()).Returns(true);
        _tokens = Substitute.For<IOpenIddictTokenManager>();
        _fences = FenceDoubles.Store("grant", "refresh", DateTimeOffset.UnixEpoch);
        _log = Substitute.For<IFenceLog>();
        _log.Append(Arg.Any<GrantFence>(), Arg.Any<CancellationToken>()).Returns(1L);
        _revocation = new(_authorizations, new GrantRevocation(_tokens, _authorizations, _fences, new(_fences, _log)));
    }

    protected async Task Revoke(string id = "grant") => _error = await Catch.Exception(() => new RevokeConnectedApp(id).Handle(_context, _revocation));
}

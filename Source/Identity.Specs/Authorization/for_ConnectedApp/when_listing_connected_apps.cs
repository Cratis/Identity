// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization.ConnectedApps;
using Cratis.Identity.Revocation;

namespace Cratis.Identity.Authorization.for_ConnectedApp;

public class when_listing_connected_apps : given.connected_grants
{
    void Establish()
    {
        var at = DateTimeOffset.Parse("2026-11-01T12:00:00Z");
        Grants(Grant("older", at), Grant("newer", at.AddDays(1)), Grant("fenced", at), Grant("dormant", at, "inactive"), Grant("revoked", at, "revoked"));
        var fence = new GrantFence(Guid.NewGuid(), 1, "fenced", Subject, RevocationReason.Revoked, at);
        _fences.Read("fenced", Arg.Any<CancellationToken>()).Returns(new GrantState("fenced", 1, null, fence, []));
        var application = new object();
        _applications.FindByIdAsync("application", Arg.Any<CancellationToken>()).Returns(application);
        _applications.GetClientIdAsync(application, Arg.Any<CancellationToken>()).Returns("calendar");
        _applications.GetDisplayNameAsync(application, Arg.Any<CancellationToken>()).Returns("Calendar");
    }

    async Task Because() => _result = (await ConnectedApp.ForCurrentAccount(_context, _reader)).ToArray();

    [Fact] void should_list_only_valid_unfenced_grants() => _result.Select(app => app.AuthorizationId).ShouldEqual(["newer", "older"]);
    [Fact] void should_read_only_the_session_subject() => _authorizations.Received(1).FindBySubjectAsync(Subject, Arg.Any<CancellationToken>());
    [Fact] void should_resolve_the_client_identifier() => _result[0].ClientId.ShouldEqual("calendar");
    [Fact] void should_resolve_the_display_name() => _result[0].DisplayName.ShouldEqual("Calendar");
    [Fact] void should_include_the_scopes() => _result[0].Scopes.ShouldEqual("openid", "profile");
    [Fact] void should_preserve_the_grant_time() => _result[0].GrantedAt.ShouldEqual(DateTimeOffset.Parse("2026-11-02T12:00:00Z"));
}

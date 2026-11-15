// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Authorization.ConnectedApps;

namespace Cratis.Identity.Authorization.for_ConnectedApp;

public class when_the_subject_is_malformed : given.connected_grants
{
    void Establish() => _context.HttpContext.User = new(new ClaimsIdentity([new(ClaimTypes.NameIdentifier, "not-an-account")], "session"));
    async Task Because() => _result = (await ConnectedApp.ForCurrentAccount(_context, _reader)).ToArray();

    [Fact] void should_list_nothing() => _result.ShouldBeEmpty();
    [Fact] void should_not_read_grants() => _authorizations.DidNotReceive().FindBySubjectAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts;
using Cratis.Identity.Authorization.ConnectedApps;

namespace Cratis.Identity.Authorization.for_ConnectedApp;

public class when_the_caller_is_anonymous : given.connected_grants
{
    Exception _error;

    void Establish() => _context.HttpContext.User = new();
    async Task Because() => _error = await Catch.Exception(() => ConnectedApp.ForCurrentAccount(_context, _reader));

    [Fact] void should_refuse_the_request() => _error.ShouldBeOfExactType<AccountOperationRefused>();
    [Fact] void should_not_read_grants() => _authorizations.DidNotReceive().FindBySubjectAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
}

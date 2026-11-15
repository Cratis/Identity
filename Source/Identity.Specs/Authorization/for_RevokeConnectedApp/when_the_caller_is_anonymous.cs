// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts;
using Cratis.Identity.Revocation;

namespace Cratis.Identity.Authorization.for_RevokeConnectedApp;

public class when_the_caller_is_anonymous : given.a_connected_app
{
    void Establish() => _context.HttpContext.User = new();
    Task Because() => Revoke();

    [Fact] void should_require_authentication() => _error.ShouldBeOfExactType<AccountOperationRefused>();
    [Fact] void should_never_commit_a_fence() => _fences.DidNotReceive().Commit(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<RevocationReason>(), Arg.Any<CancellationToken>());
}

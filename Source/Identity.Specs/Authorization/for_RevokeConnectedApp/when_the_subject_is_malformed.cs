// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Accounts;
using Cratis.Identity.Revocation;

namespace Cratis.Identity.Authorization.for_RevokeConnectedApp;

public class when_the_subject_is_malformed : given.a_connected_app
{
    void Establish() => _context.HttpContext.User = new(new ClaimsIdentity([new(ClaimTypes.NameIdentifier, "not-an-account")], "session"));
    Task Because() => Revoke();

    [Fact] void should_refuse_the_request() => _error.ShouldBeOfExactType<AccountOperationRefused>();
    [Fact] void should_never_commit_a_fence() => _fences.DidNotReceive().Commit(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<RevocationReason>(), Arg.Any<CancellationToken>());
}

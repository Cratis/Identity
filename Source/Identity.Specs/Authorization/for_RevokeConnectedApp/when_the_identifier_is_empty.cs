// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts;
using Cratis.Identity.Revocation;

namespace Cratis.Identity.Authorization.for_RevokeConnectedApp;

public class when_the_identifier_is_empty : given.a_connected_app
{
    Task Because() => Revoke(" ");

    [Fact] void should_refuse_the_request() => _error.ShouldBeOfExactType<AccountOperationRefused>();
    [Fact] void should_not_read_an_authorization() => _authorizations.DidNotReceive().FindByIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    [Fact] void should_never_commit_a_fence() => _fences.DidNotReceive().Commit(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<RevocationReason>(), Arg.Any<CancellationToken>());
}

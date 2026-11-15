// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts;
using Cratis.Identity.Revocation;

namespace Cratis.Identity.Authorization.for_RevokeConnectedApp;

public class when_the_grant_is_unknown : given.a_connected_app
{
    Task Because() => Revoke("unknown");

    [Fact] void should_give_the_same_non_disclosing_refusal() => _error.Message.ShouldEqual("connected_app_not_available");
    [Fact] void should_refuse_the_request() => _error.ShouldBeOfExactType<AccountOperationRefused>();
    [Fact] void should_never_commit_a_fence() => _fences.DidNotReceive().Commit(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<RevocationReason>(), Arg.Any<CancellationToken>());
}

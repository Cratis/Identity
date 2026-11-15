// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;

namespace Cratis.Identity.Authorization.for_RevokeConnectedApp;

public class when_the_fence_log_is_unavailable : given.a_connected_app
{
    void Establish() => _log.Append(Arg.Any<GrantFence>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<long>(new InvalidOperationException("unavailable")));
    Task Because() => Revoke();

    [Fact] void should_not_acknowledge_success() => _error.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_not_confirm_the_log() => _fences.DidNotReceive().ConfirmLog(Arg.Any<GrantFence>(), Arg.Any<CancellationToken>());
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization.ConnectedApps;

namespace Cratis.Identity.Authorization.for_ConnectedApp;

public class when_the_account_has_no_grants : given.connected_grants
{
    void Establish() => Grants();

    async Task Because() => _result = (await ConnectedApp.ForCurrentAccount(_context, _reader)).ToArray();

    [Fact] void should_return_an_empty_list() => _result.ShouldBeEmpty();
    [Fact] void should_read_only_the_session_subject() => _authorizations.Received(1).FindBySubjectAsync(Subject, Arg.Any<CancellationToken>());
}

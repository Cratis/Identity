// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization.ConnectedApps;

namespace Cratis.Identity.Authorization.for_ConnectedApp;

public class when_a_newest_grant_cannot_be_described : given.connected_grants
{
    void Establish()
    {
        var grants = Enumerable.Range(0, 1001).Select(index => Grant(index.ToString("D4"), DateTimeOffset.UnixEpoch.AddDays(index))).ToArray();
        Grants(grants);
        _authorizations.GetApplicationIdAsync(grants[^1], Arg.Any<CancellationToken>()).Returns("missing");
        var application = new object();
        _applications.FindByIdAsync("application", Arg.Any<CancellationToken>()).Returns(application);
        _applications.GetClientIdAsync(application, Arg.Any<CancellationToken>()).Returns("calendar");
    }

    async Task Because() => _result = (await ConnectedApp.ForCurrentAccount(_context, _reader)).ToArray();

    [Fact] void should_fill_the_list_from_older_candidates() => _result.Length.ShouldEqual(1000);
    [Fact] void should_skip_the_undescribable_grant() => _result[0].AuthorizationId.ShouldEqual("0999");
    [Fact] void should_continue_beyond_the_initial_thousand_candidates() => _result[^1].AuthorizationId.ShouldEqual("0000");
}

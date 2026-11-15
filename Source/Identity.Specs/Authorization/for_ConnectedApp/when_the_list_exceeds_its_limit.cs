// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization.ConnectedApps;

namespace Cratis.Identity.Authorization.for_ConnectedApp;

public class when_the_list_exceeds_its_limit : given.connected_grants
{
    void Establish()
    {
        Grants(Enumerable.Range(0, 1001).Reverse().Select(index => Grant(index.ToString("D4"), DateTimeOffset.UnixEpoch)).ToArray());
        var application = new object();
        _applications.FindByIdAsync("application", Arg.Any<CancellationToken>()).Returns(application);
        _applications.GetClientIdAsync(application, Arg.Any<CancellationToken>()).Returns("calendar");
    }

    async Task Because() => _result = (await ConnectedApp.ForCurrentAccount(_context, _reader)).ToArray();

    [Fact] void should_cap_the_display_list() => _result.Length.ShouldEqual(1000);
    [Fact] void should_break_timestamp_ties_by_authorization_id() => _result[0].AuthorizationId.ShouldEqual("0000");
    [Fact] void should_sort_before_applying_the_cap() => _result[^1].AuthorizationId.ShouldEqual("0999");
}

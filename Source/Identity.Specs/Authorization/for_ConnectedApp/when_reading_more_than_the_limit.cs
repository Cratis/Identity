// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization.ConnectedApps;

namespace Cratis.Identity.Authorization.for_ConnectedApp;

public class when_reading_more_than_the_limit : given.connected_grants
{
    object[] _grants;

    void Establish()
    {
        _grants = Enumerable.Range(0, 1005).Select(index => Grant(index.ToString("D4"), DateTimeOffset.UnixEpoch.AddDays(index))).ToArray();
        Grants(_grants);
        var application = new object();
        _applications.FindByIdAsync("application", Arg.Any<CancellationToken>()).Returns(application);
        _applications.GetClientIdAsync(application, Arg.Any<CancellationToken>()).Returns("calendar");
    }

    async Task Because() => _result = (await ConnectedApp.ForCurrentAccount(_context, _reader)).ToArray();

    [Fact] void should_return_exactly_the_limit() => _result.Length.ShouldEqual(1000);
    [Fact] void should_return_the_newest_grant_first() => _result[0].AuthorizationId.ShouldEqual("1004");
    [Fact] void should_stop_at_the_thousandth_newest_grant() => _result[^1].AuthorizationId.ShouldEqual("0005");
    [Fact] void should_bound_application_lookups() => _applications.Received(1000).FindByIdAsync("application", Arg.Any<CancellationToken>());
    [Fact] void should_bound_scope_reads() => _authorizations.Received(1000).GetScopesAsync(Arg.Any<object>(), Arg.Any<CancellationToken>());
    [Fact] void should_not_describe_older_grants() => _authorizations.DidNotReceive().GetApplicationIdAsync(Arg.Is<object>(grant => _grants.Take(5).Contains(grant)), Arg.Any<CancellationToken>());
}

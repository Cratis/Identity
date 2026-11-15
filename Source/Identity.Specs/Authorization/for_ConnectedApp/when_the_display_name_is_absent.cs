// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization.ConnectedApps;

namespace Cratis.Identity.Authorization.for_ConnectedApp;

public class when_the_display_name_is_absent : given.connected_grants
{
    void Establish()
    {
        Grants(Grant("grant", DateTimeOffset.UnixEpoch));
        var application = new object();
        _applications.FindByIdAsync("application", Arg.Any<CancellationToken>()).Returns(application);
        _applications.GetClientIdAsync(application, Arg.Any<CancellationToken>()).Returns("calendar");
    }

    async Task Because() => _result = (await ConnectedApp.ForCurrentAccount(_context, _reader)).ToArray();

    [Fact] void should_fall_back_to_the_client_identifier() => _result.Single().DisplayName.ShouldEqual("calendar");
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Cratis.Identity.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.Realm.for_IdentityRealmMiddleware;

public class when_device_authorization_is_disabled : Specification
{
    bool _dispatched;
    bool _active;

    async Task Because()
    {
        var middleware = new IdentityRealmMiddleware(
            _ =>
            {
                _dispatched = true;
                _active = IdentityRealmScope.IsActive;
                return Task.CompletedTask;
            },
            Options.Create(new ArcOptions()),
            identity: Options.Create(new IdentityOptions()));
        var context = new DefaultHttpContext();
        context.Request.Path = "/device";
        await middleware.InvokeAsync(context);
    }

    [Fact] void should_still_dispatch_the_host_request() => _dispatched.ShouldBeTrue();
    [Fact] void should_not_claim_the_hosts_device_route() => _active.ShouldBeFalse();
}

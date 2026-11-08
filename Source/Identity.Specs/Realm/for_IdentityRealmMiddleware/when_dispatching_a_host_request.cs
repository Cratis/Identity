// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.Realm.for_IdentityRealmMiddleware;

public class when_dispatching_a_host_request : Specification
{
    bool _active;

    async Task Because()
    {
        var middleware = new IdentityRealmMiddleware(
            _ =>
            {
                _active = IdentityRealmScope.IsActive;
                return Task.CompletedTask;
            },
            Options.Create(new ArcOptions()));
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/example/orders";
        await middleware.InvokeAsync(context);
    }

    [Fact] void should_not_change_the_host_scope() => _active.ShouldBeFalse();
}

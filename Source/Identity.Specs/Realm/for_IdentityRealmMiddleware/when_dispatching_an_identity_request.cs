// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Cratis.Identity.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.Realm.for_IdentityRealmMiddleware;

public class when_dispatching_an_identity_request : Specification
{
    [Theory]
    [InlineData("/connect/token")]
    [InlineData("/device")]
    [InlineData("/.well-known/openid-configuration")]
    [InlineData("/signin-github")]
    [InlineData("/api/cratis/identity/accounts/mine")]
    public async Task should_establish_the_realm_before_dispatch(string path)
    {
        var dispatched = false;
        var middleware = new IdentityRealmMiddleware(
            _ =>
            {
                dispatched = true;
                IdentityRealmScope.IsActive.ShouldBeTrue();
                return Task.CompletedTask;
            },
            Options.Create(new ArcOptions()),
            Options.Create(new IdentityOptions { DeviceGrant = new() { Enabled = true } }));
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        await middleware.InvokeAsync(context);
        dispatched.ShouldBeTrue();
        IdentityRealmScope.IsActive.ShouldBeFalse();
    }
}

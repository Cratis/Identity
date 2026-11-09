// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Identity.for_IdentityWebApplicationExtensions;

public class when_sign_out_is_enabled : given.an_identity_host
{
    [Theory]
    [InlineData("")]
    [InlineData("/id")]
    async Task should_post_the_form_to_the_prefixed_path_and_keep_antiforgery(string prefix)
    {
        var builder = CreateBuilder(prefix);
        builder.AddCratisIdentity();
        builder.Services.AddSingleton(Substitute.For<IAuthenticationSchemeProvider>());
        await using var app = builder.Build();
        app.UseCratisIdentity();
        app.MapCratisIdentity(options => options.MapSignOut = true);
        app.Run(context => context.GetEndpoint().RequestDelegate(context));
        var form = await Request(app, $"{prefix}/logout");
        form.Response.StatusCode.ShouldEqual(StatusCodes.Status200OK);
        form.Response.Headers.CacheControl.ToString().ShouldEqual("no-store");
        form.Response.Body.Position = 0;
        using var reader = new StreamReader(form.Response.Body);
        (await reader.ReadToEndAsync(TestContext.Current.CancellationToken)).ShouldContain($"action=\"{prefix}/logout\"");
        var rejected = await Request(app, $"{prefix}/logout", "POST");
        rejected.Response.StatusCode.ShouldEqual(StatusCodes.Status400BadRequest);
    }
}

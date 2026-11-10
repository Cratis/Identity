// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Identity.Device;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Identity.for_DeviceAuthorizationError;

public class when_returning_a_protocol_error : Specification
{
    [Theory]
    [InlineData("invalid_client", 401)]
    [InlineData("invalid_target", 400)]
    [InlineData("invalid_scope", 400)]
    [InlineData("temporarily_unavailable", 503)]
    public async Task should_return_uncacheable_oauth_json_without_an_authentication_forbid(string error, int status)
    {
        await using var services = new ServiceCollection().AddLogging().BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = services };
        await using var body = new MemoryStream();
        context.Response.Body = body;

        await new DeviceAuthorizationError(error, "The request cannot be completed.").ExecuteAsync(context);

        context.Response.StatusCode.ShouldEqual(status);
        context.Response.ContentType.ShouldEqual("application/json; charset=utf-8");
        context.Response.Headers.CacheControl.ToString().ShouldEqual("no-store");
        context.Response.Headers.Pragma.ToString().ShouldEqual("no-cache");
        body.Position = 0;
        using var response = await JsonDocument.ParseAsync(body, cancellationToken: TestContext.Current.CancellationToken);
        response.RootElement.GetProperty("error").GetString().ShouldEqual(error);
        response.RootElement.GetProperty("error_description").GetString().ShouldEqual("The request cannot be completed.");
        response.RootElement.TryGetProperty("access_token", out _).ShouldBeFalse();
    }
}

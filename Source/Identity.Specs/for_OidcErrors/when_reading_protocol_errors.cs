// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Identity.Integration.given;

namespace Cratis.Identity.for_OidcErrors;

public class when_reading_protocol_errors : Specification
{
    [Theory]
    [InlineData("application/json", /*lang=json*/ "{\"error\":\"invalid_scope\",\"error_description\":\"not returned\"}", "invalid_scope")]
    [InlineData("application/json", /*lang=json*/ "{}", null)]
    [InlineData("text/plain", "error:invalid_request\nerror_description:not returned\n", "invalid_request")]
    [InlineData("text/plain", "error: invalid_target\r\nerror_description: not returned\r\n", "invalid_target")]
    [InlineData("text/plain", "error_description:not an error code\n", null)]
    [InlineData("text/plain", "", null)]
    [InlineData("text/html", "error:invalid_request", null)]
    public async Task should_read_only_the_error_code(string mediaType, string body, string? expected)
    {
        using var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, mediaType)
        };

        (await OidcErrors.From(response)).ShouldEqual(expected);
    }

    [Fact]
    public async Task should_read_a_redirect_without_parsing_the_body()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.Redirect)
        {
            Content = new StringContent("not JSON", System.Text.Encoding.UTF8, "application/json")
        };
        response.Headers.Location = new Uri("https://client.example/callback?error=invalid_scope&state=synthetic-state");

        (await OidcErrors.From(response)).ShouldEqual("invalid_scope");
    }
}

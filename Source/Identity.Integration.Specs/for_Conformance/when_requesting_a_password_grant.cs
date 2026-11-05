// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.for_Conformance;

[Requirement(Requirements.NoPasswordGrant)]
[Collection(IdentityCollection.Name)]
public class when_requesting_a_password_grant(IdentityHost host) : Specification
{
    Browser _browser;
    HttpStatusCode _status;
    JsonElement _error;

    async Task Establish()
    {
        _browser = host.CreateBrowser();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/connect/token")
        {
            Content = new FormUrlEncodedContent(
            [
                new("grant_type", "password"),
                new("client_id", "cratis-cli"),
                new("username", "unused@example.test"),
                new("password", "unused")
            ])
        };
        using var response = await _browser.Client.SendAsync(request);
        _status = response.StatusCode;
        _error = await Browser.ReadJson(response);
    }

    [Fact] void should_refuse_the_password_grant() => Conformance.Conformance.Expect(Requirements.NoPasswordGrant, () => _status.ShouldEqual(HttpStatusCode.BadRequest));
    [Fact] void should_report_unsupported_grant_type() => Conformance.Conformance.Expect(Requirements.NoPasswordGrant, () => _error.GetProperty("error").GetString().ShouldEqual("unsupported_grant_type"));

    void Destroy() => _browser.Client.Dispose();
}

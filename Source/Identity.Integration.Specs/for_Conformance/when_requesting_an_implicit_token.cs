// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.for_Conformance;

[Requirement(Requirements.NoImplicit)]
[Collection(IdentityCollection.Name)]
public class when_requesting_an_implicit_token(IdentityHost host) : Specification
{
    Browser _browser;
    string _body;
    HttpStatusCode _status;

    async Task Establish()
    {
        _browser = host.CreateBrowser();
        var (_, challenge) = Browser.Pkce();
        var parameters = Browser.CliAuthorization(challenge);
        parameters.RemoveAll(_ => _.Key == "response_type");
        parameters.Add(new("response_type", "token"));
        using var response = await _browser.Authorize(parameters);
        _status = response.StatusCode;
        _body = await response.Content.ReadAsStringAsync();
    }

    [Fact] void should_refuse_the_implicit_response_type() => Conformance.Conformance.Expect(Requirements.NoImplicit, () => _status.ShouldEqual(HttpStatusCode.BadRequest));
    [Fact] void should_report_unsupported_response_type() => Conformance.Conformance.Expect(Requirements.NoImplicit, () => _body.Contains("error:unsupported_response_type", StringComparison.Ordinal).ShouldBeTrue());

    void Destroy() => _browser.Client.Dispose();
}

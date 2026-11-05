// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.for_Conformance;

[Requirement(Requirements.RevocationUnknownToken)]
[Collection(IdentityCollection.Name)]
public class when_revoking_an_unknown_token(IdentityHost host) : Specification
{
    Browser _browser;
    HttpStatusCode _status;

    async Task Establish()
    {
        _browser = host.CreateBrowser();
        (_status, _) = await _browser.Revoke($"unknown-{Guid.NewGuid():N}");
    }

    [Fact] void should_succeed_without_disclosing_token_existence() => Conformance.Conformance.Expect(Requirements.RevocationUnknownToken, () => _status.ShouldEqual(HttpStatusCode.OK));

    void Destroy() => _browser.Client.Dispose();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;
using Cratis.Identity.Device;
using Cratis.Identity.given;
using Cratis.Identity.Resources;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Cratis.Identity.for_DeviceGrantPages;

public class when_rendering_prefixed_forms : Specification
{
    [Theory]
    [InlineData("/device")]
    [InlineData("/id/device")]
    public void should_post_both_forms_to_the_verification_path(string path)
    {
        var csrf = new AntiforgeryTokenSet("request", "cookie", "antiforgery", "header");
        var resource = new ResourceCatalog(Configurations.TwoResourceServers().AsOptions()).FindByAudience(Configurations.DirectAudience);
        var entry = (ContentHttpResult)DeviceGrantPages.Entry(csrf, "issuer.example", path);
        var review = (ContentHttpResult)DeviceGrantPages.Review(new("client", new Grant(resource, ["direct:read"]), "code", "proof", [], csrf, "issuer.example", "client-id", "login", path));
        entry.ResponseContent.ShouldContain($"action=\"{path}\"");
        review.ResponseContent.ShouldContain($"action=\"{path}\"");
    }
}

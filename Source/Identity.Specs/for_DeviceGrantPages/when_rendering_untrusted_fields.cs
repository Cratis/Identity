// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;
using Cratis.Identity.Device;
using Cratis.Identity.Resources;
using Cratis.Identity.Tenants;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Cratis.Identity.for_DeviceGrantPages;

public class when_rendering_untrusted_fields : Specification
{
    const string Untrusted = "<script>\"&'";
    const string Encoded = "&lt;script&gt;&quot;&amp;&#x27;";
    string _html;

    void Because()
    {
        var csrf = new AntiforgeryTokenSet(Untrusted, "cookie", Untrusted, "header");
        var resource = new ResourceServer(Untrusted, Untrusted, ["https://resource.example"], [new(Untrusted, Untrusted)]);
        var grant = new Grant(resource, [Untrusted]);
        _html = ((ContentHttpResult)DeviceGrantPages.Review(new(Untrusted, grant, Untrusted, Untrusted, [new SelectableTenant(Untrusted, Untrusted)], csrf, Untrusted, Untrusted, Untrusted, Untrusted))).ResponseContent;
    }

    [Theory]
    [InlineData("<strong>", "</strong>")]
    [InlineData("<code>", "</code>")]
    [InlineData("</code> ", "</li>")]
    [InlineData("<p>Client: ", " <code>")]
    [InlineData("<p>Resource: ", " (")]
    [InlineData(" (", ")</p>")]
    [InlineData("<form method=\"post\" action=\"", "\"")]
    [InlineData("<input type=\"hidden\" name=\"user_code\" value=\"", "\"")]
    [InlineData("<input type=\"hidden\" name=\"proof\" value=\"", "\"")]
    [InlineData("<input type=\"hidden\" name=\"", "\"")]
    [InlineData("<option value=\"", "\"")]
    [InlineData("<option value=\"&lt;script&gt;&quot;&amp;&#x27;\">", "</option>")]
    public void should_encode_fields_at_the_html_boundary(string before, string after) => _html.ShouldContain(before + Encoded + after);

    [Fact]
    void should_never_render_raw_field_values() => _html.ShouldNotContain(Untrusted);
}

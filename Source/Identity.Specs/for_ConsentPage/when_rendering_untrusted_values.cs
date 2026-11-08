// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts;
using Cratis.Identity.Authorization;
using Cratis.Identity.given;
using Cratis.Identity.Resources;
using Cratis.Identity.Tenants;

namespace Cratis.Identity.for_ConsentPage;

public class when_rendering_untrusted_values : Specification
{
    const string Script = "<script>alert(1)</script>";
    string _html;

    void Because()
    {
        var direct = new ResourceCatalog(Configurations.TwoResourceServers().AsOptions()).FindByAudience(Configurations.DirectAudience);
        _html = ConsentPage.Render(
            new Consent(
                "cratis-cli",
                Script,
                new Uri("http://127.0.0.1:53123/callback"),
                new Grant(direct, ["direct:read"]),
                [new SelectableTenant("cratis", Script)],
                new Account(new("7d9d5c9e-0000-4000-8000-000000000001"), "Name", null, "1", Script, AccountStatus.Active, []),
                [new("state", "\"><script>alert(2)</script>")]),
            "token");
    }

    [Fact] void should_not_contain_a_script_element() => _html.Contains("<script", StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
    [Fact] void should_describe_a_loopback_redirect_as_a_local_program() => _html.Contains("a program on this computer (127.0.0.1:53123)", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_carry_the_antiforgery_token() => _html.Contains("name=\"__consent_antiforgery\" value=\"token\"", StringComparison.Ordinal).ShouldBeTrue();
}

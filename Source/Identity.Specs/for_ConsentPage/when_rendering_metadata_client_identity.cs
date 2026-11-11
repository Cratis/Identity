// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts;
using Cratis.Identity.Authorization;
using Cratis.Identity.Resources;

namespace Cratis.Identity.for_ConsentPage;

public class when_rendering_metadata_client_identity : Specification
{
    [Fact]
    void should_separate_unverified_name_from_the_ascii_client_identity()
    {
        var html = ConsentPage.Render(Model(new Uri("http://127.0.0.1:49152/callback")), "synthetic-antiforgery");
        html.ShouldContain("Client identity: xn--caf-dma.example");
        html.ShouldContain("Name provided by the client (not verified):");
        html.ShouldContain("Any local program can use this loopback client identity.");
        html.ShouldContain("&lt;script&gt;");
        html.ShouldNotContain("<script>");
        html.ShouldNotContain("\u202e", StringComparison.Ordinal);
        html.ShouldNotContain("\u200b", StringComparison.Ordinal);
        html.ShouldNotContain("href=\"https://xn--caf-dma.example/client.json\"");
    }

    [Fact]
    void should_only_warn_about_local_programs_for_loopback_redirects()
    {
        var html = ConsentPage.Render(Model(new Uri("https://client.example/callback")), "synthetic-antiforgery");
        html.ShouldContain("Name provided by the client (not verified):");
        html.ShouldNotContain("Any local program");
    }

    [Fact]
    void should_leave_configured_client_text_unchanged()
    {
        var html = ConsentPage.Render(Model(new Uri("http://127.0.0.1:49152/callback")) with { ClientIdentityHost = null }, "synthetic-antiforgery");
        html.ShouldNotContain("Client identity:");
        html.ShouldNotContain("Name provided by the client");
        html.ShouldNotContain("Any local program");
    }

    static Consent Model(Uri redirect) => new(
        "https://xn--caf-dma.example/client.json",
        " <script>\u202e\u200b ",
        redirect,
        new Grant(new ResourceServer("direct-api", "Direct", ["https://direct.example/mcp"], [new ScopeDefinition("direct:read", "Read")]), ["direct:read"]),
        [],
        new Account(new("7d9d5c9e-0000-4000-8000-000000000001"), "Synthetic Member", null, "42", "synthetic-member", AccountStatus.Active, []),
        [],
        ClientIdentityHost: "xn--caf-dma.example");
}

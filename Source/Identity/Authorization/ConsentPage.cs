// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Encodings.Web;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.Authorization;

/// <summary>
/// Renders the consent page as self-contained HTML with no scripts.
/// </summary>
internal static class ConsentPage
{
    const string Style =
        "body{font-family:system-ui,sans-serif;max-width:36rem;margin:3rem auto;padding:0 1rem;color:#1b1b1f;line-height:1.5}" +
        "h1{font-size:1.4rem}dt{font-weight:600;margin-top:.75rem}dd{margin:0}fieldset{border:1px solid #ccc;border-radius:.5rem;margin:1rem 0}" +
        ".warn{background:#fff4e5;border-radius:.5rem;padding:.75rem}.error{color:#b00020;font-weight:600}" +
        "button{font-size:1rem;padding:.5rem 1.25rem;margin-right:.5rem;border-radius:.4rem;border:1px solid #444}.allow{background:#1b1b1f;color:#fff}";

    /// <summary>
    /// Renders the consent page.
    /// </summary>
    /// <param name="consent">The <see cref="Consent"/> to render.</param>
    /// <param name="antiforgeryToken">The antiforgery request token.</param>
    /// <returns>The HTML.</returns>
    public static string Render(Consent consent, string antiforgeryToken)
    {
        var html = new StringBuilder()
            .Append("<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"utf-8\">")
            .Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"><meta name=\"referrer\" content=\"no-referrer\">")
            .Append("<title>Authorize ").Append(E(consent.ClientName)).Append(" - Cratis</title><style>").Append(Style).Append("</style></head><body>")
            .Append("<h1>Authorize ").Append(E(consent.ClientName)).Append("?</h1>")
            .Append("<p>Signed in as <strong>").Append(E(consent.Account.GitHubLogin.Value)).Append("</strong> (GitHub).</p>")
            .Append("<p class=\"warn\">Only continue if you started this sign-in yourself. The client identifies itself as <code>")
            .Append(E(consent.ClientId)).Append("</code>; Identity cannot prove which program is asking.</p>")
            .Append("<dl><dt>Access will be sent to</dt><dd>").Append(E(DescribeRedirect(consent.RedirectUri))).Append("</dd>")
            .Append("<dt>Access to</dt><dd>").Append(E(consent.Grant.Resource.DisplayName))
            .Append(" <code>").Append(E(consent.Grant.Resource.Audience)).Append("</code></dd>")
            .Append("<dt>Permissions</dt><dd><ul>");

        foreach (var scope in consent.Grant.Resource.Scopes.Where(_ => consent.Grant.Scopes.Contains(_.Name, StringComparer.Ordinal)))
        {
            html.Append("<li><code>").Append(E(scope.Name)).Append("</code> ").Append(E(scope.Description)).Append("</li>");
        }

        if (consent.Grant.Scopes.Contains(Scopes.OfflineAccess, StringComparer.Ordinal))
        {
            html.Append("<li>Stay signed in until you sign out or revoke access</li>");
        }

        html.Append("</ul></dd></dl><form method=\"post\">");

        foreach (var (name, value) in consent.Parameters)
        {
            Hidden(html, name, value);
        }

        Hidden(html, ConsentForm.AntiforgeryField, antiforgeryToken);
        AppendTenants(html, consent);

        html.Append("<button class=\"allow\" type=\"submit\" name=\"").Append(ConsentForm.Decision).Append("\" value=\"").Append(ConsentForm.Allow).Append("\">Allow</button>")
            .Append("<button type=\"submit\" name=\"").Append(ConsentForm.Decision).Append("\" value=\"").Append(ConsentForm.Deny).Append("\" formnovalidate>Deny</button>")
            .Append("</form></body></html>");

        return html.ToString();
    }

    /// <summary>
    /// Describes where the authorization code will be sent, so the user can tell a local program from a website.
    /// </summary>
    /// <param name="redirectUri">The redirect URI.</param>
    /// <returns>The description.</returns>
    public static string DescribeRedirect(Uri redirectUri) =>
        redirectUri.IsLoopback
            ? $"a program on this computer ({redirectUri.Authority})"
            : redirectUri.Authority;

    static void AppendTenants(StringBuilder html, Consent consent)
    {
        html.Append("<fieldset><legend>Tenant</legend>");
        if (consent.Error is not null)
        {
            html.Append("<p class=\"error\">").Append(E(consent.Error)).Append("</p>");
        }

        var tenants = consent.Tenants.ToArray();
        var hints = consent.Parameters.Where(_ => string.Equals(_.Key, "tenant", StringComparison.Ordinal)).Select(_ => _.Value).ToArray();
        var hintedTenant = hints.Length == 1 && tenants.Any(_ => string.Equals(_.Id, hints[0], StringComparison.Ordinal)) ? hints[0] : null;
        var selectedTenant = hintedTenant ?? (tenants.Length == 1 ? tenants[0].Id : null);
        foreach (var tenant in tenants)
        {
            html.Append("<label><input type=\"radio\" name=\"").Append(ConsentForm.Tenant)
                .Append("\" value=\"").Append(E(tenant.Id)).Append('"')
                .Append(string.Equals(tenant.Id, selectedTenant, StringComparison.Ordinal) ? " checked" : string.Empty)
                .Append(" required> ").Append(E(tenant.Name)).Append("</label><br>");
        }

        html.Append("</fieldset>");
    }

    static void Hidden(StringBuilder html, string name, string value) =>
        html.Append("<input type=\"hidden\" name=\"").Append(E(name)).Append("\" value=\"").Append(E(value)).Append("\">");

    static string E(string value) => HtmlEncoder.Default.Encode(value);
}

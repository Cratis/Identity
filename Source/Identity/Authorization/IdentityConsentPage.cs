// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Encodings.Web;

namespace Cratis.Identity.Authorization;

/// <summary>Renders explicit identity-only consent, without remembered approval or tenant fields.</summary>
internal static class IdentityConsentPage
{
    /// <summary>Renders a no-script consent form.</summary>
    /// <param name="consent">The trusted client and requested identity scopes.</param>
    /// <param name="antiforgeryToken">The session-bound request token.</param>
    /// <returns>Encoded consent HTML.</returns>
    public static string Render(IdentityConsent consent, string antiforgeryToken)
    {
        var html = new StringBuilder("<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"utf-8\"><meta name=\"referrer\" content=\"no-referrer\"><title>Authorize sign-in - Cratis</title></head><body><h1>Authorize ")
            .Append(E(consent.ClientName)).Append("?</h1><p>Only continue if you started this sign-in yourself.</p><p>Sign-in will return to ")
            .Append(E(consent.RedirectUri.Authority)).Append(".</p><ul>");
        foreach (var scope in consent.Grant.Scopes) html.Append("<li><code>").Append(E(scope)).Append("</code></li>");
        html.Append("</ul><form method=\"post\">");
        foreach (var parameter in consent.Parameters) Hidden(html, parameter.Key, parameter.Value);
        Hidden(html, ConsentForm.AntiforgeryField, antiforgeryToken);
        html.Append("<button type=\"submit\" name=\"").Append(ConsentForm.Decision).Append("\" value=\"").Append(ConsentForm.Allow).Append("\">Allow</button>")
            .Append("<button type=\"submit\" name=\"").Append(ConsentForm.Decision).Append("\" value=\"").Append(ConsentForm.Deny).Append("\">Deny</button></form></body></html>");
        return html.ToString();
    }

    static void Hidden(StringBuilder html, string name, string value) => html.Append("<input type=\"hidden\" name=\"").Append(E(name)).Append("\" value=\"").Append(E(value)).Append("\">");
    static string E(string value) => HtmlEncoder.Default.Encode(value);
}

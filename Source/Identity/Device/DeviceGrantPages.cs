// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Encodings.Web;
using Cratis.Identity.Authorization;
using Cratis.Identity.Tenants;
using Microsoft.AspNetCore.Antiforgery;

namespace Cratis.Identity.Device;

/// <summary>Minimal server-rendered, script-free device verification pages.</summary>
internal static class DeviceGrantPages
{
    const string Warning = "<p>Only enter a code shown on a device you are signing in on. Never enter a code someone else sent you.</p>";

    /// <summary>Renders manual code entry without reflecting untrusted input.</summary>
    /// <param name="csrf">The session's antiforgery token.</param>
    /// <returns>The entry page.</returns>
    /// <param name="issuer">The trusted issuer host.</param>
    internal static IResult Entry(AntiforgeryTokenSet csrf, string issuer) => Page(
        Header(issuer) +
        "<h1>Connect a device</h1>" +
        "<p>Enter the code shown on the device you are connecting.</p>" +
        Warning +
        "<form method=\"post\" action=\"/device\">" + Csrf(csrf) +
        "<input name=\"user_code\" maxlength=\"64\" autocomplete=\"off\" required>" +
        "<button name=\"action\" value=\"review\">Review request</button></form>");

    /// <summary>Renders the exact client, audience and scopes being approved.</summary>
    /// <param name="client">The client display name.</param>
    /// <param name="grant">The admitted grant.</param>
    /// <param name="code">The canonical entered code.</param>
    /// <param name="proof">The session-bound consent evidence.</param>
    /// <param name="tenants">The live selectable tenants.</param>
    /// <param name="csrf">The session's antiforgery token.</param>
    /// <returns>The confirmation page.</returns>
    /// <param name="issuer">The trusted issuer host.</param>
    /// <param name="clientId">The asserted public client identifier.</param>
    /// <param name="login">The signed-in GitHub login.</param>
    internal static IResult Review(string client, Grant grant, string code, string proof, IEnumerable<SelectableTenant> tenants, AntiforgeryTokenSet csrf, string issuer, string clientId, string login) => Page(
        Header(issuer) +
        $"<h1>Approve device access</h1><p>Client: {Encode(client)} <code>{Encode(clientId)}</code></p>" +
        $"<p>Signed in as <strong>{Encode(login)}</strong> (GitHub).</p>" +
        $"<p>Resource: {Encode(grant.Resource.DisplayName)} ({Encode(grant.Resource.Audience)})</p>" +
        "<ul>" + ScopeDescriptions(grant) + "</ul>" +
        Warning +
        "<form method=\"post\" action=\"/device\">" + Csrf(csrf) +
        $"<input type=\"hidden\" name=\"user_code\" value=\"{Encode(code)}\">" +
        $"<input type=\"hidden\" name=\"proof\" value=\"{Encode(proof)}\">" +
        "<label>Tenant <select name=\"tenant\" required>" + TenantOptions(tenants) + "</select></label>" +
        "<button name=\"action\" value=\"approve\">Approve</button>" +
        "<button name=\"action\" value=\"deny\">Deny</button></form>");

    /// <summary>Returns the same safe failure for unknown, expired or otherwise unusable codes.</summary>
    /// <returns>The failure page.</returns>
    internal static IResult Unavailable() => Page("<h1>Unable to complete verification</h1><p>That code is invalid or has expired. Check the code on your device or start again.</p>", StatusCodes.Status400BadRequest);

    /// <summary>Returns an explicit throttle refusal without reflecting request values.</summary>
    /// <returns>The rate-limit page.</returns>
    internal static IResult Throttled() => Page("<h1>Too many attempts</h1><p>Wait before trying again.</p>", StatusCodes.Status429TooManyRequests);

    /// <summary>Renders completion without carrying any credential in a redirect.</summary>
    /// <returns>The completion page.</returns>
    internal static IResult Complete() => Page("<h1>Verification complete</h1><p>Return to your device to continue.</p>");

    static string Header(string issuer) => $"<p>Verify you are on <strong>{Encode(issuer)}</strong>.</p>";

    static string ScopeDescriptions(Grant grant) => string.Concat(grant.Resource.Scopes.Where(_ => grant.Scopes.Contains(_.Name, StringComparer.Ordinal)).Select(_ => $"<li><code>{Encode(_.Name)}</code> {Encode(_.Description)}</li>")) +
        (grant.Scopes.Contains("offline_access", StringComparer.Ordinal) ? "<li>Stay signed in until you sign out or revoke access</li>" : string.Empty);

    static string TenantOptions(IEnumerable<SelectableTenant> tenants) => string.Concat(tenants.Select(_ => $"<option value=\"{Encode(_.Id)}\">{Encode(_.Name)}</option>"));

    static string Csrf(AntiforgeryTokenSet csrf) => $"<input type=\"hidden\" name=\"{Encode(csrf.FormFieldName)}\" value=\"{Encode(csrf.RequestToken ?? string.Empty)}\">";

    static string Encode(string value) => HtmlEncoder.Default.Encode(value);

    static IResult Page(string body, int status = StatusCodes.Status200OK) => Results.Content($"<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\"><title>Device verification</title></head><body>{body}</body></html>", "text/html", statusCode: status);
}

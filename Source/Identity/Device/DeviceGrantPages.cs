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
    /// <param name="path">The prefixed verification path.</param>
    internal static IResult Entry(AntiforgeryTokenSet csrf, string issuer, string path = "/device") => Page(
        Header(issuer) +
        "<h1>Connect a device</h1>" +
        "<p>Enter the code shown on the device you are connecting.</p>" +
        Warning +
        $"<form method=\"post\" action=\"{Encode(path)}\">" + Csrf(csrf) +
        "<input name=\"user_code\" maxlength=\"64\" autocomplete=\"off\" required>" +
        "<button name=\"action\" value=\"review\">Review request</button></form>");

    /// <summary>Renders the exact client, audience and scopes being approved.</summary>
    /// <param name="review">The review fields, encoded at the HTML boundary.</param>
    /// <returns>The confirmation page.</returns>
    internal static IResult Review(ReviewFields review) => Page(
        Header(review.Issuer) +
        $"<h1>Approve device access</h1><p>Client: {Encode(review.Client)} <code>{Encode(review.ClientId)}</code></p>" +
        $"<p>Signed in as <strong>{Encode(review.Login)}</strong> (GitHub).</p>" +
        $"<p>Resource: {Encode(review.Grant.Resource.DisplayName)} ({Encode(review.Grant.Resource.Audience)})</p>" +
        "<ul>" + ScopeDescriptions(review.Grant) + "</ul>" +
        Warning +
        $"<form method=\"post\" action=\"{Encode(review.Path)}\">" + Csrf(review.Csrf) +
        $"<input type=\"hidden\" name=\"user_code\" value=\"{Encode(review.Code)}\">" +
        $"<input type=\"hidden\" name=\"proof\" value=\"{Encode(review.Proof)}\">" +
        "<label>Tenant <select name=\"tenant\" required>" + TenantOptions(review.Tenants) + "</select></label>" +
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

    /// <summary>The unencoded fields of a device approval review.</summary>
    /// <param name="Client">The client display name.</param>
    /// <param name="Grant">The admitted grant.</param>
    /// <param name="Code">The canonical entered code.</param>
    /// <param name="Proof">The session-bound consent evidence.</param>
    /// <param name="Tenants">The live selectable tenants.</param>
    /// <param name="Csrf">The session's antiforgery token.</param>
    /// <param name="Issuer">The trusted issuer host.</param>
    /// <param name="ClientId">The asserted public client identifier.</param>
    /// <param name="Login">The signed-in GitHub login.</param>
    /// <param name="Path">The prefixed verification path.</param>
    internal sealed record ReviewFields(string Client, Grant Grant, string Code, string Proof, IEnumerable<SelectableTenant> Tenants, AntiforgeryTokenSet Csrf, string Issuer, string ClientId, string Login, string Path = "/device");
}

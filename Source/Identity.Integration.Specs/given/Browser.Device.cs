// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.given;

public partial class Browser
{
    /// <summary>Starts a real public-client device request.</summary>
    /// <param name="scope">The requested scopes.</param>
    /// <param name="resource">The requested resource.</param>
    /// <returns>The protocol response.</returns>
    public Task<(HttpStatusCode Status, JsonElement Body)> StartDevice(string scope = "direct:read offline_access", string resource = IdentityHost.DirectMcp) => PostForm("/connect/device",
    [
        new("client_id", ClientId),
        new("scope", scope),
        new("resource", resource)
    ]);

    /// <summary>Polls the token endpoint with a device code.</summary>
    /// <param name="code">The opaque device code.</param>
    /// <param name="client">The client identifier.</param>
    /// <returns>The protocol response.</returns>
    public Task<(HttpStatusCode Status, JsonElement Body)> PollDevice(string code, string client = ClientId) => PostForm("/connect/token",
    [
        new("client_id", client),
        new("grant_type", "urn:ietf:params:oauth:grant-type:device_code"),
        new("device_code", code)
    ]);

    /// <summary>Submits an explicit verification action without putting codes into URLs.</summary>
    /// <param name="page">The preceding verification page.</param>
    /// <param name="action">Review, approve or deny.</param>
    /// <param name="code">The code, only for the initial entry form.</param>
    /// <param name="includeAntiforgery">Whether to send the antiforgery token.</param>
    /// <param name="includeProof">Whether to send the approval proof.</param>
    /// <returns>The raw final response.</returns>
    public Task<HttpResponseMessage> VerifyDevice(string page, string action, string? code = null, bool includeAntiforgery = true, bool includeProof = true)
    {
        var fields = HiddenInputs.Matches(page)
            .Select(_ => new KeyValuePair<string, string>(WebUtility.HtmlDecode(_.Groups["name"].Value), WebUtility.HtmlDecode(_.Groups["value"].Value)))
            .Where(_ => (includeAntiforgery || _.Key != "__consent_antiforgery") && (includeProof || _.Key != "proof"))
            .Append(new("action", action)).Append(new("tenant", IdentityHost.MemberTenant)).ToList();
        if (code is not null) fields.Add(new("user_code", code));
        return Send(new HttpRequestMessage(HttpMethod.Post, "/device") { Content = new FormUrlEncodedContent(fields) });
    }

    /// <summary>Signs in, enters a code, and returns the real confirmation page.</summary>
    /// <param name="code">The user code returned to the device.</param>
    /// <returns>The confirmation HTML.</returns>
    public async Task<string> ReviewDevice(string code)
    {
        using var entry = await Send(new HttpRequestMessage(HttpMethod.Get, "/device"));
        entry.EnsureSuccessStatusCode();
        using var review = await VerifyDevice(await entry.Content.ReadAsStringAsync(), "review", code);
        review.EnsureSuccessStatusCode();
        return await review.Content.ReadAsStringAsync();
    }
}

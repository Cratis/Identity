// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Sessions;

/// <summary>The end-session parameters, with duplicate parameter names refused.</summary>
/// <param name="Hint">The optional ID-token hint.</param>
/// <param name="ClientId">The optional client identifier.</param>
/// <param name="RedirectUri">The optional post-logout redirect URI.</param>
/// <param name="State">The optional relying-party state.</param>
internal record EndSessionRequest(string? Hint, string? ClientId, string? RedirectUri, string? State)
{
    static readonly string[] _parameters = ["id_token_hint", "client_id", "post_logout_redirect_uri", "state"];

    /// <summary>Reads GET query or POST form parameters without accepting ambiguous duplicates.</summary>
    /// <param name="request">The HTTP request.</param>
    /// <returns>The request, or null for an unsupported or ambiguous request.</returns>
    public static async Task<EndSessionRequest?> Read(HttpRequest request)
    {
        var values = request.Query.AsEnumerable();
        if (HttpMethods.IsPost(request.Method))
        {
            if (!request.HasFormContentType)
            {
                return null;
            }

            values = values.Concat(await request.ReadFormAsync(request.HttpContext.RequestAborted));
        }

        var fields = values.Where(_ => _parameters.Contains(_.Key, StringComparer.Ordinal)).ToArray();
        if (fields.Any(_ => _.Value.Count != 1) || fields.GroupBy(_ => _.Key, StringComparer.Ordinal).Any(_ => _.Count() != 1))
        {
            return null;
        }

        var parameters = fields.ToDictionary(_ => _.Key, _ => _.Value[0], StringComparer.Ordinal);
        return new(parameters.GetValueOrDefault("id_token_hint"), parameters.GetValueOrDefault("client_id"), parameters.GetValueOrDefault("post_logout_redirect_uri"), parameters.GetValueOrDefault("state"));
    }
}

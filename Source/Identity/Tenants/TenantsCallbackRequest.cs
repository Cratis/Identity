// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Cratis.Identity.Tenants;

/// <summary>
/// Represents the internal, interim product callback request; personal identifiers stay out of the URL.
/// </summary>
/// <param name="Audience">The registered resource audience.</param>
/// <param name="Subject">The canonical account identifier.</param>
/// <param name="GitHubId">The legacy GitHub membership key.</param>
/// <param name="Idp">The exact issuer of the selected GitHub membership link.</param>
/// <param name="IdpSub">The immutable subject of the selected GitHub membership link.</param>
internal record TenantsCallbackRequest(
    [property: JsonPropertyName("audience")] string Audience,
    [property: JsonPropertyName("subject")] string Subject,
    [property: JsonPropertyName("github_id")] string GitHubId,
    [property: JsonPropertyName("idp")] string Idp,
    [property: JsonPropertyName("idp_sub")] string IdpSub);

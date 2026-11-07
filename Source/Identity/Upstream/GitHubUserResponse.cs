// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Identity.Accounts;
using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Upstream;

/// <summary>
/// Reads the GitHub REST API <c language="text">GET /user</c> response.
/// </summary>
public static class GitHubUserResponse
{
    /// <summary>
    /// Reads a <see cref="GitHubUser"/> from a <c language="text">GET /user</c> response.
    /// </summary>
    /// <param name="user">The response body.</param>
    /// <returns>The <see cref="GitHubUser"/>, or <see langword="null"/> when the response lacks a numeric id or a login.</returns>
    public static GitHubUser? Read(JsonElement user)
    {
        if (user.ValueKind != JsonValueKind.Object ||
            !user.TryGetProperty("id", out var id) || id.ValueKind != JsonValueKind.Number || !id.TryGetInt64(out var gitHubId) || gitHubId <= 0 ||
            !user.TryGetProperty("login", out var login) || login.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(login.GetString()))
        {
            return null;
        }

        var name = user.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(n.GetString())
            ? n.GetString()!
            : login.GetString()!;

        var email = user.TryGetProperty("email", out var emailProperty) && emailProperty.ValueKind == JsonValueKind.String
            ? emailProperty.GetString()
            : null;
        return new GitHubUser(gitHubId, login.GetString()!, name)
        {
            Email = string.IsNullOrWhiteSpace(email) ? null : new Email(email)
        };
    }
}

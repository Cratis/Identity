// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Identity.Accounts;
using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Upstream;

/// <summary>
/// Reads the GitHub REST API <c language="text">GET /user</c> response.
/// </summary>
internal static class GitHubUserResponse
{
    /// <summary>
    /// Reads a <see cref="GitHubUser"/> from a <c language="text">GET /user</c> response.
    /// </summary>
    /// <param name="user">The response body.</param>
    /// <returns>The <see cref="GitHubUser"/>, or <see langword="null"/> when the response lacks a numeric id or a login.</returns>
    public static GitHubUser? Read(JsonElement user)
    {
        if (user.ValueKind != JsonValueKind.Object ||
            InvalidId(user, out var gitHubId) ||
            InvalidLogin(user, out var login))
        {
            return null;
        }

        var name = ReadName(user, login);
        var email = ReadEmail(user);
        return new GitHubUser(gitHubId, login.GetString()!, name)
        {
            Email = string.IsNullOrWhiteSpace(email) ? null : new Email(email)
        };
    }

    static bool InvalidId(JsonElement user, out long gitHubId)
    {
        gitHubId = 0;
        return !user.TryGetProperty("id", out var id) || id.ValueKind != JsonValueKind.Number || !id.TryGetInt64(out gitHubId) || gitHubId <= 0;
    }

    static bool InvalidLogin(JsonElement user, out JsonElement login) =>
        !user.TryGetProperty("login", out login) || login.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(login.GetString());

    static string ReadName(JsonElement user, JsonElement login) =>
        user.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(n.GetString())
            ? n.GetString()!
            : login.GetString()!;

    static string? ReadEmail(JsonElement user) =>
        user.TryGetProperty("email", out var emailProperty) && emailProperty.ValueKind == JsonValueKind.String
            ? emailProperty.GetString()
            : null;
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Clients;

/// <summary>Minimum admission strength for command-only confidential secrets.</summary>
internal static class ClientSecretStrength
{
    /// <summary>Requires at least 16 characters and rejects whitespace-only material.</summary>
    /// <param name="secret">The command-only secret.</param>
    /// <returns>Whether the minimum length policy holds; this does not measure entropy.</returns>
    public static bool IsValid(string? secret) => secret is { Length: >= 16 } && !string.IsNullOrWhiteSpace(secret);
}

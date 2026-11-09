// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Revocation;

/// <summary>
/// Refuses a Chronicle workload credential whose secret equals a people-realm secret. It catches value reuse only:
/// who issued the credential is an infrastructure fact this check cannot prove.
/// </summary>
internal static class ChronicleCredentialSeparation
{
    /// <summary>Requires the Chronicle connection credential to differ from every realm secret.</summary>
    /// <param name="chronicleConnectionString">The Chronicle connection string, which may carry a client secret.</param>
    /// <param name="realmSecrets">The people-realm secrets such as the upstream and token exchange client secrets.</param>
    /// <exception cref="RevocationPending">The Chronicle credential reuses a realm secret.</exception>
    internal static void Require(string? chronicleConnectionString, IEnumerable<string?> realmSecrets)
    {
        var credentials = Credentials(chronicleConnectionString);
        if (realmSecrets.Any(secret => !string.IsNullOrEmpty(secret) && credentials.Contains(secret, StringComparer.Ordinal)))
        {
            throw new RevocationPending();
        }
    }

    static string[] Credentials(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString) || !Uri.TryCreate(connectionString, UriKind.Absolute, out var uri))
        {
            return [];
        }

        var fromUserInfo = uri.UserInfo.Split(':').Select(Uri.UnescapeDataString);
        var fromQuery = uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split('=', 2)).Where(pair => pair.Length == 2).Select(pair => Uri.UnescapeDataString(pair[1]));
        return fromUserInfo.Concat(fromQuery).Where(value => value.Length > 0).ToArray();
    }
}

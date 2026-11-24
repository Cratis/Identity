// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Security.Cryptography;
using System.Text;

namespace Cratis.Identity.Clients.Serving;

/// <summary>Verifies a secret a client presented against the digests its registration holds.</summary>
internal static class RegisteredClientSecrets
{
    /// <summary>Checks the secret against every digest that is valid at the given time.</summary>
    /// <param name="registration">The client's registration.</param>
    /// <param name="secret">The secret the client presented.</param>
    /// <param name="digester">Digests the presented secret the way registered ones were.</param>
    /// <param name="at">The time to check at.</param>
    /// <returns>Whether the secret is the current one, or the previous one while its overlap window lasts.</returns>
    internal static bool IsValid(ClientRegistration registration, string secret, ClientSecretDigester digester, DateTimeOffset at)
    {
        if (string.IsNullOrEmpty(secret))
        {
            return false;
        }

        var presented = Encoding.UTF8.GetBytes(digester.Digest(secret).Value);
        var valid = false;

        // Every candidate is compared, so how long this takes does not tell which one matched.
        foreach (var candidate in registration.ValidSecretHashesAt(at))
        {
            valid |= CryptographicOperations.FixedTimeEquals(presented, Encoding.UTF8.GetBytes(candidate.Value));
        }

        return valid;
    }
}

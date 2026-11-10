// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Clients.Registering;

/// <summary>D3 protocol identifier admission, without normalization or case folding.</summary>
internal static class ClientIdRules
{
    /// <summary>Requires 1..128 RFC 6749 VSCHAR characters, excluding colon and edge whitespace.</summary>
    /// <param name="id">The proposed exact identifier.</param>
    /// <returns>Whether the identifier is admitted.</returns>
    public static bool IsValid(ClientId? id)
    {
        var value = id?.Value;
        if (value is not { Length: > 0 and <= 128 })
        {
            return false;
        }

        return value[0] != ' ' && value[^1] != ' ' && value.All(character => character is >= ' ' and <= '~' and not ':');
    }
}

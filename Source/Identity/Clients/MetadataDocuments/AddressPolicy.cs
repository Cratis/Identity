// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;

namespace Cratis.Identity.Clients.MetadataDocuments;

/// <summary>Conservative public-unicast allow boundary; translated addresses are never public here.</summary>
internal static class AddressPolicy
{
    static readonly (IPAddress Network, int Prefix)[] _reservedV4 =
    [
        (IPAddress.Parse("0.0.0.0"), 8), (IPAddress.Parse("10.0.0.0"), 8),
        (IPAddress.Parse("100.64.0.0"), 10), (IPAddress.Parse("127.0.0.0"), 8),
        (IPAddress.Parse("169.254.0.0"), 16), (IPAddress.Parse("172.16.0.0"), 12),
        (IPAddress.Parse("192.0.0.0"), 24), (IPAddress.Parse("192.0.2.0"), 24),
        (IPAddress.Parse("192.88.99.0"), 24), (IPAddress.Parse("192.168.0.0"), 16),
        (IPAddress.Parse("198.18.0.0"), 15), (IPAddress.Parse("198.51.100.0"), 24),
        (IPAddress.Parse("203.0.113.0"), 24), (IPAddress.Parse("224.0.0.0"), 3)
    ];

    static readonly (IPAddress Network, int Prefix)[] _reservedV6 =
    [
        (IPAddress.Parse("2001::"), 23), (IPAddress.Parse("2001:db8::"), 32),
        (IPAddress.Parse("2002::"), 16), (IPAddress.Parse("3fff::"), 20)
    ];

    /// <summary>Tests one DNS answer. Every answer must pass before any connection is made.</summary>
    /// <param name="address">The resolved address.</param>
    /// <returns>Whether the address is unambiguous public unicast.</returns>
    public static bool IsPublic(IPAddress address) => address.AddressFamily switch
    {
        AddressFamily.InterNetwork => !_reservedV4.Any(_ => Contains(address, _.Network, _.Prefix)),
        AddressFamily.InterNetworkV6 => address.ScopeId == 0 && Contains(address, IPAddress.Parse("2000::"), 3) &&
            !_reservedV6.Any(_ => Contains(address, _.Network, _.Prefix)),
        _ => false
    };

    static bool Contains(IPAddress address, IPAddress network, int prefix)
    {
        var bytes = address.GetAddressBytes();
        var range = network.GetAddressBytes();
        var full = prefix / 8;
        var remainder = prefix % 8;
        return bytes.AsSpan(0, full).SequenceEqual(range.AsSpan(0, full)) &&
            (remainder == 0 || (bytes[full] >> (8 - remainder)) == (range[full] >> (8 - remainder)));
    }
}

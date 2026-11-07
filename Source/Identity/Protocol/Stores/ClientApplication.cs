// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using OpenIddict.Abstractions;

namespace Cratis.Identity.Protocol.Stores;

/// <summary>A configuration-backed application with an obfuscated client secret.</summary>
public sealed class ClientApplication
{
    /// <summary>Initializes a snapshot without retaining the clear client secret.</summary>
    /// <param name="descriptor">The declared client descriptor.</param>
    internal ClientApplication(OpenIddictApplicationDescriptor descriptor)
    {
        Descriptor = descriptor;
        Descriptor.ClientSecret = string.IsNullOrEmpty(descriptor.ClientSecret) ? null : ClientSecretHash.Create(descriptor.ClientSecret);
        ClientId = descriptor.ClientId!;

        // Mongo's operational authorization/token stores require a 12-byte application identifier.
        // Deriving it from the declared client keeps new entries stable across process restarts.
        Id = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(ClientId)).AsSpan(0, 12));
    }

    /// <summary>Gets the stable identifier used by operational token and authorization entries.</summary>
    public string Id { get; }

    /// <summary>Gets the declared client identifier.</summary>
    public string ClientId { get; }

    /// <summary>Gets the private descriptor snapshot containing only an obfuscated secret.</summary>
    internal OpenIddictApplicationDescriptor Descriptor { get; }
}

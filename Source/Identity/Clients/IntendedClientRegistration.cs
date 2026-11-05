// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Clients.Secrets;
using Cratis.Identity.Contracts;
using Cratis.Identity.Contracts.Clients;
using OpenIddict.Abstractions;

namespace Cratis.Identity.Clients;

/// <summary>Validated startup intent; only typed non-secret values can become facts.</summary>
/// <param name="Policy">The configured policy descriptor.</param>
/// <param name="ClientId">The validated OAuth identifier.</param>
/// <param name="RedirectUris">The validated redirect set.</param>
/// <param name="Digest">The optional keyed secret comparand.</param>
internal record IntendedClientRegistration(OpenIddictApplicationDescriptor Policy, ClientId ClientId, RedirectUri[] RedirectUris, SecretDigest? Digest)
{
    /// <summary>Validates a descriptor without appending facts.</summary>
    /// <param name="descriptor">The configured client policy.</param>
    /// <param name="digester">The secret-custody digester.</param>
    /// <returns>The validated registration intent.</returns>
    /// <exception cref="InvalidClientValue">The descriptor contains invalid client configuration.</exception>
    internal static IntendedClientRegistration Create(OpenIddictApplicationDescriptor descriptor, ClientSecretDigester digester)
    {
        var id = ClientId.Create(descriptor.ClientId!);
        var redirects = descriptor.RedirectUris.Select(uri => RedirectUri.Create(uri.OriginalString)).Distinct().ToArray();
        SecretDigest? digest = null;
        if (descriptor.ClientType == OpenIddictConstants.ClientTypes.Confidential)
        {
            if (string.IsNullOrEmpty(descriptor.ClientSecret))
            {
                throw new InvalidClientValue("confidential client secret");
            }

            digest = digester.Digest(id, descriptor.ClientSecret);
        }

        return new(descriptor, id, redirects, digest);
    }
}

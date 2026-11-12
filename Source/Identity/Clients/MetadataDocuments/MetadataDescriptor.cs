// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Identity.Resources;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.Clients.MetadataDocuments;

/// <summary>Server-owned public-client policy; document permissions never flow into this policy.</summary>
internal static class MetadataDescriptor
{
    /// <summary>The validated provenance marker used by consent rendering.</summary>
    public const string Provenance = "identity:cimd";

    /// <summary>Creates the fixed code, refresh, S256 and explicit-consent shape.</summary>
    /// <param name="metadata">The validated public metadata.</param>
    /// <returns>A descriptor with no document-controlled authority.</returns>
    public static OpenIddictApplicationDescriptor Create(ClientIdMetadataDocument metadata)
    {
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = metadata.ClientId.OriginalString,
            DisplayName = metadata.ClientName,
            ClientType = ClientTypes.Public,
            ApplicationType = metadata.Native ? ApplicationTypes.Native : ApplicationTypes.Web,
            ConsentType = ConsentTypes.Explicit,
            Permissions =
            {
                Permissions.Endpoints.Authorization, Permissions.Endpoints.Token, Permissions.Endpoints.Revocation,
                Permissions.GrantTypes.AuthorizationCode, Permissions.ResponseTypes.Code
            },
            Requirements = { Requirements.Features.ProofKeyForCodeExchange },
            Properties = { [Provenance] = JsonSerializer.SerializeToElement(true) }
        };
        descriptor.RedirectUris.UnionWith(metadata.RedirectUris);
        if (metadata.AllowsRefreshToken) descriptor.Permissions.Add(Permissions.GrantTypes.RefreshToken);
        return descriptor;
    }

    /// <summary>Applies only the issuer's resource and scope catalogue, never a document scope request.</summary>
    /// <param name="descriptor">The validated public descriptor.</param>
    /// <param name="resources">The issuer catalogue.</param>
    public static void ApplyResources(OpenIddictApplicationDescriptor descriptor, IResourceCatalog resources)
    {
        foreach (var resource in resources.All)
        {
            descriptor.Permissions.UnionWith(resource.Scopes.Where(_ => _.Name != Scopes.OpenId).Select(_ => Permissions.Prefixes.Scope + _.Name));
            descriptor.Permissions.UnionWith(resource.Indicators.Select(_ => Permissions.Prefixes.Resource + _));
        }
    }

    /// <summary>Detaches mutable OpenIddict collections at every cache boundary.</summary>
    /// <param name="source">The validated snapshot.</param>
    /// <returns>An independent descriptor.</returns>
    public static OpenIddictApplicationDescriptor Copy(OpenIddictApplicationDescriptor source)
    {
        var copy = new OpenIddictApplicationDescriptor
        {
            ClientId = source.ClientId,
            DisplayName = source.DisplayName,
            ApplicationType = source.ApplicationType,
            ClientType = source.ClientType,
            ConsentType = source.ConsentType
        };
        copy.RedirectUris.UnionWith(source.RedirectUris);
        copy.Permissions.UnionWith(source.Permissions);
        copy.Requirements.UnionWith(source.Requirements);
        foreach (var property in source.Properties) copy.Properties.Add(property.Key, property.Value.Clone());
        return copy;
    }
}

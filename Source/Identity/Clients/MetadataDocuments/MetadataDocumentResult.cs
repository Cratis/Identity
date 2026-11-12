// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OpenIddict.Abstractions;

namespace Cratis.Identity.Clients.MetadataDocuments;

/// <summary>A validated descriptor and freshness, or a refusal without untrusted content.</summary>
/// <param name="Descriptor">The validated descriptor, absent on refusal.</param>
/// <param name="Refusal">The reason code.</param>
/// <param name="Lifetime">The response freshness; zero prohibits reuse.</param>
internal sealed record MetadataDocumentResult(OpenIddictApplicationDescriptor? Descriptor, MetadataRefusal Refusal, TimeSpan? Lifetime = null)
{
    /// <summary>Creates a refusal without retaining any remote content.</summary>
    /// <param name="reason">The reason code.</param>
    /// <returns>The refused result.</returns>
    public static MetadataDocumentResult Refused(MetadataRefusal reason) => new(null, reason);
}

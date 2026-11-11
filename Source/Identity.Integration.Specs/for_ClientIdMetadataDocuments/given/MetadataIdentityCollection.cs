// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.for_ClientIdMetadataDocuments.given;

/// <summary>Shares the real issuer stores for metadata conformance; CI only.</summary>
[CollectionDefinition(Name)]
public class MetadataIdentityCollection : ICollectionFixture<MetadataIdentityHost>
{
    public const string Name = "Identity CIMD";
}

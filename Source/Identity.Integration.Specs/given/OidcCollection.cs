// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.given;

/// <summary>Shares the enabled OIDC host and its store across protocol specs.</summary>
[CollectionDefinition(Name)]
public class OidcCollection : ICollectionFixture<OidcIdentityHost>
{
    public const string Name = "OIDC";
}

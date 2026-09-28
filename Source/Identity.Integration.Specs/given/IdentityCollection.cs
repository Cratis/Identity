// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.given;

/// <summary>
/// Shares one <see cref="IdentityHost"/> (and one MongoDB container) across the integration specs.
/// </summary>
[CollectionDefinition(Name)]
public class IdentityCollection : ICollectionFixture<IdentityHost>
{
    public const string Name = "Identity";
}

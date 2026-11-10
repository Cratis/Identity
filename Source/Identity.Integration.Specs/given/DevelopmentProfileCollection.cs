// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.given;

/// <summary>
/// Shares one <see cref="DevelopmentProfileHost"/> across the local-development profile specs.
/// </summary>
[CollectionDefinition(Name)]
public class DevelopmentProfileCollection : ICollectionFixture<DevelopmentProfileHost>
{
    public const string Name = "DevelopmentProfile";
}

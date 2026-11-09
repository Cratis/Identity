// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.given;

/// <summary>Shares the opt-in host only among DPoP protocol scenarios.</summary>
[CollectionDefinition(Name)]
public class DPoPCollection : ICollectionFixture<DPoPHost>
{
    public const string Name = "DPoP";
}

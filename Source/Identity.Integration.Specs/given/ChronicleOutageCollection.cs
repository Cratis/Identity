// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.given;

/// <summary>Keeps deliberate Chronicle outages and their recovery state away from ordinary protocol specs.</summary>
[CollectionDefinition(Name)]
public class ChronicleOutageCollection : ICollectionFixture<IdentityHost>
{
    public const string Name = "Chronicle outage";
}

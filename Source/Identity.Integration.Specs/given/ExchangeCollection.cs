// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.given;

/// <summary>
/// Gives token-exchange specs their own host, so enabling exchange cannot disturb the shared one.
/// </summary>
[CollectionDefinition(Name)]
public class ExchangeCollection : ICollectionFixture<ExchangeIdentityHost>
{
    public const string Name = "Exchange";
}

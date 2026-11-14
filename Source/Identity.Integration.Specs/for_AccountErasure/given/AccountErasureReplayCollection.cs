// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.for_AccountErasure.given;

/// <summary>
/// Isolates account projection replay in its own host and kernel, including when replay fails.
/// </summary>
[CollectionDefinition(Name)]
public class AccountErasureReplayCollection : ICollectionFixture<IdentityHost>
{
    public const string Name = "Identity account erasure replay";
}

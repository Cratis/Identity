// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.given;

/// <summary>Keeps the read-model replay specs on their own kernel, so a stuck replay cannot hold up sign-in catch-up elsewhere.</summary>
[CollectionDefinition(Name)]
public class ErasureReplayCollection : ICollectionFixture<ErasureReplayHost>
{
    public const string Name = "Erasure replay";
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Cratis.Identity.Operational;

/// <summary>Durable key-check metadata, separate from event payloads and never secret material.</summary>
/// <param name="Id">The singleton id, or a legacy numeric revision id.</param>
/// <param name="Check">The domain-separated key check.</param>
/// <param name="Revision">The retained key revision.</param>
public record LookupRevision(BsonValue Id, string Check, int Revision = 0)
{
    /// <summary>The identity shared by every instance of the startup guard.</summary>
    public const string SingletonId = "lookup-key-revision";

    /// <summary>Gets the revision, including for legacy one-document-per-revision metadata.</summary>
    [BsonIgnore]
    public int RetainedRevision => Id.IsInt32 ? Id.AsInt32 : Revision;
}

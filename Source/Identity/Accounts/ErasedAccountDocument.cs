// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson.Serialization.Attributes;

namespace Cratis.Identity.Accounts;

/// <summary>
/// Represents the minimal marker left behind when an account document is erased. It holds no personal data.
/// </summary>
public class ErasedAccountDocument
{
    /// <summary>
    /// Gets or sets the identifier of the erased account.
    /// </summary>
    [BsonId]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the account document was erased.
    /// </summary>
    [BsonElement("erased_at")]
    public DateTimeOffset ErasedAt { get; set; }
}

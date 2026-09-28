// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson.Serialization.Attributes;

namespace Cratis.Identity.Storage;

/// <summary>
/// Represents a stored Data Protection key.
/// </summary>
public class DataProtectionKeyDocument
{
    /// <summary>
    /// Gets or sets the key's friendly name.
    /// </summary>
    [BsonId]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the key XML.
    /// </summary>
    [BsonElement("xml")]
    public string Xml { get; set; } = string.Empty;
}

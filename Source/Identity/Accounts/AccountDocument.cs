// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson.Serialization.Attributes;

namespace Cratis.Identity.Accounts;

/// <summary>
/// Represents the stored shape of an <see cref="Account"/>.
/// </summary>
internal class AccountDocument
{
    /// <summary>
    /// Gets or sets the canonical account identifier.
    /// </summary>
    [BsonId]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the numeric GitHub user identifier.
    /// </summary>
    [BsonElement("github_id")]
    public long GitHubId { get; set; }

    /// <summary>
    /// Gets or sets the GitHub login.
    /// </summary>
    [BsonElement("github_login")]
    public string GitHubLogin { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the account was created.
    /// </summary>
    [BsonElement("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets when the account last signed in.
    /// </summary>
    [BsonElement("last_signed_in_at")]
    public DateTimeOffset LastSignedInAt { get; set; }

    /// <summary>
    /// Converts the document to an <see cref="Account"/>.
    /// </summary>
    /// <returns>The <see cref="Account"/>.</returns>
    public Account ToAccount() => new(Id, GitHubId, GitHubLogin, Name);
}

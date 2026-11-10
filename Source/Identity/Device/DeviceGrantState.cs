// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson.Serialization.Attributes;

namespace Cratis.Identity.Device;

/// <summary>One bounded operational rate window; identifiers are purpose-separated keyed digests.</summary>
internal sealed class DeviceGrantState
{
    /// <summary>Gets or sets the digest, never a credential or a personal identifier.</summary>
    [BsonId]
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the CAS revision.</summary>
    public long Revision { get; set; }

    /// <summary>Gets or sets the verification count.</summary>
    public int Attempts { get; set; }

    /// <summary>Gets or sets the next permitted poll.</summary>
    public DateTime NextPoll { get; set; }

    /// <summary>Gets or sets the current interval in seconds.</summary>
    public int Interval { get; set; }

    /// <summary>Gets or sets the logical expiry, also used for TTL cleanup.</summary>
    public DateTime ExpiresAt { get; set; }
}

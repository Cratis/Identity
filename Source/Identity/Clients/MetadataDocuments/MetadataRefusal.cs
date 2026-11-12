// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Clients.MetadataDocuments;

/// <summary>Bounded, non-sensitive metadata outcome categories.</summary>
internal enum MetadataRefusal
{
    /// <summary>The document was accepted.</summary>
    None = 0,

    /// <summary>The client identifier is not a permitted canonical URL.</summary>
    InvalidUrl = 1,

    /// <summary>A DNS answer is not public unicast.</summary>
    Address = 2,

    /// <summary>The HTTP transport failed.</summary>
    Transport = 3,

    /// <summary>The total deadline elapsed.</summary>
    Timeout = 4,

    /// <summary>The status is not 200.</summary>
    Status = 5,

    /// <summary>The media type or content encoding is not supported.</summary>
    ContentType = 6,

    /// <summary>The document exceeds its byte bound.</summary>
    Size = 7,

    /// <summary>The document is not valid bounded UTF-8 JSON.</summary>
    Json = 8,

    /// <summary>The document violates the public-client policy.</summary>
    Metadata = 9,

    /// <summary>The bounded entry capacity is exhausted.</summary>
    Capacity = 10,

    /// <summary>Only unauthenticated public clients are supported.</summary>
    UnsupportedAuthMethod = 11,

    /// <summary>Fetch admission has no rate or concurrency capacity.</summary>
    RateLimited = 12
}

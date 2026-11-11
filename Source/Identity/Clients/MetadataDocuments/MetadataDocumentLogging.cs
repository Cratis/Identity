// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Clients.MetadataDocuments;

/// <summary>Reason-only logging: no remote bodies, URL paths, headers or resolved addresses.</summary>
internal static partial class MetadataDocumentLogging
{
    /// <summary>Records one metadata lookup without retaining document content.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="host">The canonical ASCII host.</param>
    /// <param name="reason">The bounded outcome.</param>
    /// <param name="elapsedMilliseconds">The lookup duration.</param>
    /// <param name="cacheHit">Whether a validated cached revision was used.</param>
    [LoggerMessage(EventId = 5901, Level = LogLevel.Debug, Message = "CIMD lookup: host {Host}, reason {Reason}, duration {ElapsedMilliseconds} ms, cache hit {CacheHit}")]
    public static partial void Lookup(this ILogger logger, string host, MetadataRefusal reason, long elapsedMilliseconds, bool cacheHit);

    /// <summary>Records consent presentation with a registration kind, not client-provided claims.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="host">The canonical ASCII host.</param>
    [LoggerMessage(EventId = 5902, Level = LogLevel.Debug, Message = "Consent for RegistrationKind ClientIdentifierMetadataDocument, host {Host}")]
    public static partial void Consent(this ILogger logger, string host);
}

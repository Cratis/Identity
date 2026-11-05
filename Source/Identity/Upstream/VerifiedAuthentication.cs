// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Upstream;

/// <summary>Immutable key vouched for by a connection adapter, not by arbitrary claims.</summary>
public sealed class VerifiedAuthentication
{
    internal VerifiedAuthentication(ConnectionId connectionId, UpstreamIssuer issuer, UpstreamSubject subject, DateTimeOffset authTime, string? sessionId = null)
    {
        ConnectionId = connectionId;
        IdpIssuer = issuer;
        Subject = subject;
        AuthTime = authTime;
        SessionId = sessionId;
    }

    /// <summary>Gets the configured connection.</summary>
    public ConnectionId ConnectionId { get; }

    /// <summary>Gets the exact authority.</summary>
    public UpstreamIssuer IdpIssuer { get; }

    /// <summary>Gets the verified immutable subject.</summary>
    public UpstreamSubject Subject { get; }

    /// <summary>Gets when the upstream proved authentication.</summary>
    public DateTimeOffset AuthTime { get; }

    internal string? SessionId { get; }

    /// <inheritdoc/>
    public override string ToString() => "Verified upstream authentication [redacted]";
}

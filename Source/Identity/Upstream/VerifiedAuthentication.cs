// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Sessions;

namespace Cratis.Identity.Upstream;

/// <summary>Immutable key vouched for by a connection adapter, not by arbitrary claims.</summary>
internal sealed class VerifiedAuthentication
{
    /// <summary>Initializes a new instance of the <see cref="VerifiedAuthentication"/> class.</summary>
    /// <param name="connectionId">The trusted connection.</param>
    /// <param name="issuer">The exact authority.</param>
    /// <param name="subject">The immutable subject.</param>
    /// <param name="authTime">The authentication instant.</param>
    /// <param name="operationId">The callback operation reference, carried unchanged through retries.</param>
    internal VerifiedAuthentication(ConnectionId connectionId, UpstreamIssuer issuer, UpstreamSubject subject, DateTimeOffset authTime, FactId operationId)
    {
        ConnectionId = connectionId;
        IdpIssuer = issuer;
        Subject = UpstreamSubject.Create(subject.Value);
        AuthTime = authTime;
        OperationId = operationId;
    }

    /// <summary>Gets the configured connection.</summary>
    public ConnectionId ConnectionId { get; }

    /// <summary>Gets the exact authority.</summary>
    public UpstreamIssuer IdpIssuer { get; }

    /// <summary>Gets the verified immutable subject.</summary>
    public UpstreamSubject Subject { get; }

    /// <summary>Gets when the upstream proved authentication.</summary>
    public DateTimeOffset AuthTime { get; }

    /// <summary>Gets the accepted operation reference, stable across retries.</summary>
    public FactId OperationId { get; }

    /// <summary>Gets the existing-account session binding.</summary>
    internal SessionId? SessionId { get; init; }

    /// <inheritdoc/>
    public override string ToString() => "Verified upstream authentication [redacted]";
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Upstream;

namespace Cratis.Identity.Accounts.Linking;

/// <summary>
/// Holds trusted observations in the command scope, supplied exactly once by the adapter.
/// </summary>
internal sealed class LinkCeremony : ILinkCeremony
{
    bool _supplied;

    /// <inheritdoc/>
    public ClaimsPrincipal Session { get; private set; } = new();

    /// <inheritdoc/>
    public VerifiedAuthentication? Authentication { get; private set; }

    /// <inheritdoc/>
    public LinkIntent Intent { get; private set; } = new(false);

    /// <inheritdoc/>
    public CancellationToken CancellationToken { get; private set; }

    /// <summary>
    /// Supplies the trusted observations exactly once within the command scope.
    /// </summary>
    /// <param name="session">The existing authenticated account principal.</param>
    /// <param name="authentication">The adapter-verified upstream proof, if required.</param>
    /// <param name="intent">The explicit ceremony intent and trusted connection policy.</param>
    /// <param name="cancellationToken">The originating request's cancellation.</param>
    /// <exception cref="AccountOperationRefused">The scope already contains a link ceremony.</exception>
    public void Set(ClaimsPrincipal session, VerifiedAuthentication? authentication, LinkIntent intent, CancellationToken cancellationToken)
    {
        AccountOperationRefused.Require(!_supplied, "link_ceremony_already_supplied");
        Session = session;
        Authentication = authentication;
        Intent = intent;
        CancellationToken = cancellationToken;
        _supplied = true;
    }
}

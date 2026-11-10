// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Upstream;

namespace Cratis.Identity.Accounts.Linking;

/// <summary>
/// Server-supplied observations for one link ceremony, never command properties.
/// </summary>
internal interface ILinkCeremony
{
    ClaimsPrincipal Session { get; }
    VerifiedAuthentication? Authentication { get; }
    LinkIntent Intent { get; }
    CancellationToken CancellationToken { get; }
}

/// <summary>
/// Holds trusted observations in the command scope, supplied exactly once by the adapter.
/// </summary>
internal sealed class LinkCeremony : ILinkCeremony
{
    bool _supplied;

    public ClaimsPrincipal Session { get; private set; } = new();
    public VerifiedAuthentication? Authentication { get; private set; }
    public LinkIntent Intent { get; private set; } = new(false);
    public CancellationToken CancellationToken { get; private set; }

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

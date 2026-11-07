// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts;

/// <summary>The raw key profile inside its owning account's erasure boundary.</summary>
/// <param name="LinkId">The opaque link.</param>
/// <param name="ConnectionId">The configured connection.</param>
/// <param name="IdpIssuer">The exact issuer.</param>
/// <param name="IdpSub">The immutable raw subject.</param>
/// <param name="Login">The mutable display login.</param>
/// <param name="LinkedAt">The link time.</param>
public record LinkedUpstreamKey(LinkId LinkId, ConnectionId ConnectionId, UpstreamIssuer IdpIssuer, UpstreamSubject IdpSub, Login Login, DateTimeOffset LinkedAt);

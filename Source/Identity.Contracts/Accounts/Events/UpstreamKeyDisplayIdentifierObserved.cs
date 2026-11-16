// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Identity.Contracts.Accounts.Events;

/// <summary>A key's mutable display identifier changed (Internal).</summary>
/// <remarks>Display and preferred_username only; never matching, linking, or authorization. Inside the erasure boundary.</remarks>
/// <param name="LinkId">The confirmed link.</param>
/// <param name="Login">The protected display identifier.</param>
/// <param name="ObservedAt">The observation time.</param>
[EventType]
public record UpstreamKeyDisplayIdentifierObserved(LinkId LinkId, Login Login, DateTimeOffset ObservedAt);

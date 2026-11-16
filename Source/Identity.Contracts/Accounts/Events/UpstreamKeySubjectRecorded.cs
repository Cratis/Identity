// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Identity.Contracts.Accounts.Events;

/// <summary>The raw subject of a confirmed link was recorded (Internal).</summary>
/// <remarks>Appended atomically with UpstreamKeyLinked, inside the account erasure boundary.</remarks>
/// <param name="LinkId">The confirmed link.</param>
/// <param name="IdpIssuer">The exact authority.</param>
/// <param name="IdpSub">The protected raw subject.</param>
[EventType]
public record UpstreamKeySubjectRecorded(LinkId LinkId, UpstreamIssuer IdpIssuer, UpstreamSubject IdpSub);

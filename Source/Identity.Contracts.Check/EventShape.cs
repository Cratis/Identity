// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Contracts.Check;

/// <summary>The declared schema of one event generation.</summary>
/// <param name="Name">The CLR event name.</param>
/// <param name="TypeId">The stable Chronicle identity.</param>
/// <param name="Generation">The generation.</param>
/// <param name="Marker">The discovery marker.</param>
/// <param name="Visibility">The effective publication status.</param>
/// <param name="ComplianceSubject">Whether an account compliance subject is resolved.</param>
/// <param name="Members">The declared members.</param>
/// <param name="ExplicitId">Whether the event declares an explicit attribute id instead of the default name.</param>
internal sealed record EventShape(
    string Name,
    string TypeId,
    uint Generation,
    string Marker,
    string Visibility,
    bool ComplianceSubject,
    IReadOnlyList<MemberShape> Members,
    bool ExplicitId = false);

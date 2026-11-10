// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Contracts.Resources.Events;

/// <summary>Internal source of ResourceScopeRetired on the resource stream.</summary>
/// <param name="ScopeName">The permanently retired scope.</param>
/// <param name="RetiredAt">The server retirement instant.</param>
/// <param name="FactId">The retry-stable fact identity, Internal only.</param>
[EventType]
public record ResourceScopeRetirementRecorded(string ScopeName, DateTimeOffset RetiredAt, FactId FactId);

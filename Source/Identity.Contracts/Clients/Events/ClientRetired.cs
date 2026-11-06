// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Contracts.Clients.Events;

/// <summary>Internal client retirement fact, replacing configuration deletion.</summary>
/// <param name="ReasonId">An allowlisted reason reference.</param>
/// <param name="RetiredAt">The retirement time.</param>
[EventType("626c1b31-29b1-47c2-946f-74f937f910a7", generation: 1)]
public record ClientRetired(ReasonId ReasonId, DateTimeOffset RetiredAt);

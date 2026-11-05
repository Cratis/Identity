// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Identity.Contracts.Accounts.Events;

/// <summary>The account's profile name changed (Internal). See the #26 identity-events-v1 catalogue.</summary>
/// <param name="PersonalName">The protected profile name.</param>
[EventType("2eba996c-8d19-4d5c-9648-3dca02daf879", generation: 1)]
public record AccountNameChanged(PersonalName PersonalName);

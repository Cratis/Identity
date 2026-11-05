// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Clients;

namespace Cratis.Identity.Clients.Registration;

/// <summary>An active client-id lookup hint, never write authority.</summary>
/// <param name="Id">The opaque stream identity.</param>
/// <param name="ClientId">The OAuth identity.</param>
public record ClientIdLookup(ClientRegistrationId Id, ClientId ClientId);

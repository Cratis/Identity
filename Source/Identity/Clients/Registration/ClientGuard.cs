// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Clients.Registration;

/// <summary>A raw-source projection guarding every client fact used in a decision.</summary>
/// <param name="Id">The opaque stream identity.</param>
public record ClientGuard(string Id);

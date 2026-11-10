// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Clients.Registering;

/// <summary>Trusted registration context read before deciding, never supplied by the command caller.</summary>
/// <param name="At">The server registration instant.</param>
/// <param name="ConfiguredClientExists">Whether an exact id is reserved by the configured descriptors.</param>
internal sealed record ClientRegistrationDecision(DateTimeOffset At, bool ConfiguredClientExists);

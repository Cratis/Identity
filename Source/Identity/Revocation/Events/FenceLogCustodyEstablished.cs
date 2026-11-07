// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Identity.Revocation.Events;

/// <summary>Fixed-key custody binding; a different log instance conflicts instead of silently replacing history.</summary>
/// <param name="InstanceId">Externally recorded independent log identity.</param>
[EventType("b90e65de-fd11-43c9-b032-e6f73d5ca089", generation: 1)]
internal sealed record FenceLogCustodyEstablished(string InstanceId);

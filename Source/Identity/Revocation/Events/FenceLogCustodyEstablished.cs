// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Identity.Revocation.Events;

/// <summary>Fixed-key custody binding; a different log instance conflicts instead of silently replacing history.</summary>
/// <param name="InstanceId">Externally recorded independent log identity.</param>
[EventType]
internal sealed record FenceLogCustodyEstablished(string InstanceId);

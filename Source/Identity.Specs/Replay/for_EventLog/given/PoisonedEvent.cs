// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Identity.Replay.for_EventLog.given;

[EventType("50f00000-0000-0000-0000-000000000001")]
internal record PoisonedEvent(string Value);

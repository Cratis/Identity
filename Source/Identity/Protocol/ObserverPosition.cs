// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;

namespace Cratis.Identity.Protocol;

/// <summary>A projection's progress, including states in which waiting cannot succeed.</summary>
/// <param name="RunningState">The observer state.</param>
/// <param name="HasFailedPartitions">Whether any partition has failed.</param>
/// <param name="IsReplaying">Whether the projection is replaying.</param>
/// <param name="LastHandled">The last handled sequence number.</param>
public record ObserverPosition(ObserverRunningState RunningState, bool HasFailedPartitions, bool IsReplaying, EventSequenceNumber LastHandled);

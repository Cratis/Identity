// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Revocation.Events;

/// <summary>A complete fence prefix, appended only after every restriction intent was durably delivered.</summary>
/// <param name="InstanceId">Independent custody identity.</param>
/// <param name="FenceSequence">The complete published prefix.</param>
[EventType]
internal sealed record FencePublicationCheckpoint(string InstanceId, FenceSequence FenceSequence);

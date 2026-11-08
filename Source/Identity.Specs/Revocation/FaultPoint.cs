// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.RevocationSpecs;

/// <summary>The steps of the revoke path after which a crash can be injected.</summary>
internal enum FaultPoint
{
    AfterReserve = 0,
    AfterCommit = 1,
    AfterLogAppend = 2,
    AfterConfirm = 3,
    AfterDelivery = 4
}

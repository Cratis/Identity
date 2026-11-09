// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Revocation;

/// <summary>Durable delivery progress; no pending fence fact can reach Chronicle.</summary>
public enum OutboxState
{
    /// <summary>The fence log has not confirmed the restriction.</summary>
    Pending = 0,

    /// <summary>The independent port has acknowledged, or no fence is required.</summary>
    LogConfirmed = 1,

    /// <summary>A keyed Chronicle append succeeded.</summary>
    Delivered = 2
}

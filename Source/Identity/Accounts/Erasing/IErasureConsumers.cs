// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Accounts.Erasing;

/// <summary>Lists the consumers erasure follow-up covers. Which consumers exist and their duty is an open policy decision.</summary>
internal interface IErasureConsumers
{
    /// <summary>Gets the listed consumers.</summary>
    IReadOnlyList<ErasureConsumer> Listed { get; }
}

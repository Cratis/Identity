// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Accounts.Erasing;

/// <summary>The follow-up state of one erasure across its listed consumers.</summary>
/// <param name="Acknowledged">Listed consumers that acknowledged.</param>
/// <param name="Pending">Consumers owing an acknowledgement that has not arrived.</param>
/// <param name="RetentionOnly">Consumers documented as retaining data.</param>
internal record ErasureConsumerStatus(IReadOnlyList<ErasureConsumerId> Acknowledged, IReadOnlyList<ErasureConsumerId> Pending, IReadOnlyList<ErasureConsumerId> RetentionOnly)
{
    /// <summary>Gets a value indicating whether every consumer owing an acknowledgement has given one.</summary>
    public bool Settled => Pending.Count == 0;
}

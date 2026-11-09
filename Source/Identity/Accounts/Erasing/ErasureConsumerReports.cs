// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.Erasing;

/// <summary>
/// Reports consumer follow-up for an erasure. It is informational and operational: nothing in the erasure workflow
/// waits on it, so an absent or late consumer can never block or fail an erasure.
/// </summary>
/// <param name="consumers">The listed consumers.</param>
/// <param name="acknowledgements">The acknowledgement record.</param>
internal class ErasureConsumerReports(IErasureConsumers consumers, IErasureAcknowledgements acknowledgements)
{
    /// <summary>Reports the follow-up state. Acknowledgements from unlisted consumers are ignored.</summary>
    /// <param name="erasure">The erasure reference.</param>
    /// <returns>The status.</returns>
    public async Task<ErasureConsumerStatus> Report(ErasureId erasure)
    {
        var received = (await acknowledgements.Acknowledged(erasure)).ToHashSet();
        var listed = consumers.Listed;
        var owing = listed.Where(consumer => consumer.Duty == ErasureConsumerDuty.DeleteAndAcknowledge).Select(consumer => consumer.Id).ToArray();
        return new(
            owing.Where(received.Contains).ToArray(),
            owing.Where(id => !received.Contains(id)).ToArray(),
            listed.Where(consumer => consumer.Duty == ErasureConsumerDuty.RetentionOnly).Select(consumer => consumer.Id).ToArray());
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.Erasing;

/// <summary>
/// The operational record of consumer acknowledgements. It is not an event and carries no personal data: only the
/// opaque erasure reference, the consumer and a time.
/// </summary>
internal interface IErasureAcknowledgements
{
    /// <summary>Records that a consumer reported its cleanup for an erasure. Repeating it is harmless.</summary>
    /// <param name="erasure">The erasure reference.</param>
    /// <param name="consumer">The acknowledging consumer.</param>
    /// <param name="at">When it acknowledged.</param>
    /// <returns>Awaitable task.</returns>
    Task Record(ErasureId erasure, ErasureConsumerId consumer, DateTimeOffset at);

    /// <summary>Reads the consumers that acknowledged an erasure.</summary>
    /// <param name="erasure">The erasure reference.</param>
    /// <returns>The acknowledging consumers.</returns>
    Task<IReadOnlyCollection<ErasureConsumerId>> Acknowledged(ErasureId erasure);
}

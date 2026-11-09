// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Accounts.Erasing;

/// <summary>The default listing: no consumer is decided, so nothing is listed.</summary>
internal class UnlistedErasureConsumers : IErasureConsumers
{
    /// <inheritdoc/>
    public IReadOnlyList<ErasureConsumer> Listed => [];
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Clients.Registering;

/// <summary>Reads trusted registration context from the clock and read-only configured clients.</summary>
/// <param name="time">The server clock.</param>
/// <param name="descriptors">Configured identifiers, independent of Internal registration streams.</param>
internal sealed class ClientRegistrationReads(TimeProvider time, ClientDescriptors descriptors)
{
    /// <summary>Reads the exact configured reservation without normalizing the proposed identifier.</summary>
    /// <param name="clientId">The proposed protocol identifier.</param>
    /// <returns>The trusted registration decision context.</returns>
    public ClientRegistrationDecision Read(ClientId? clientId) => new(
        time.GetUtcNow(), descriptors.All().Any(descriptor => string.Equals(descriptor.ClientId, clientId?.Value, StringComparison.Ordinal)));
}

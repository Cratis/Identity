// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Cratis.Chronicle.EventSequences;

namespace Cratis.Identity.Clients.Requests;

/// <summary>What Cratis Identity decided to do about a request from a product.</summary>
/// <param name="Events">The internal facts to append, empty when the request is refused.</param>
/// <param name="Refusal">The closed reason the request was refused, or <see langword="null"/> when it is accepted.</param>
internal sealed record ClientRequestOutcome(IReadOnlyList<EventForEventSourceId> Events, string? Refusal)
{
    /// <summary>Gets a value indicating whether the request is accepted.</summary>
    public bool Accepted => Refusal is null;

    /// <summary>Creates an accepted outcome.</summary>
    /// <param name="events">The facts to append.</param>
    /// <returns>The outcome.</returns>
    public static ClientRequestOutcome Accept(IReadOnlyList<EventForEventSourceId> events) => new(events, null);

    /// <summary>Creates a refused outcome.</summary>
    /// <param name="reason">The closed reason, never caller text.</param>
    /// <returns>The outcome.</returns>
    public static ClientRequestOutcome Refuse(string reason) => new([], reason);
}

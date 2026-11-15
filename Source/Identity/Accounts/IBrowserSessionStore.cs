// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Accounts;

/// <summary>
/// Stores <see cref="BrowserSession"/> records so a session cookie is only honored while its record stands.
/// </summary>
internal interface IBrowserSessionStore
{
    /// <summary>Records a new browser session at sign-in.</summary>
    /// <param name="session">The <see cref="BrowserSession"/>.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task Record(BrowserSession session, CancellationToken cancellationToken);

    /// <summary>Finds a session by its identifier.</summary>
    /// <param name="sessionId">The identifier carried by the session cookie.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The <see cref="BrowserSession"/>, or null when none is recorded.</returns>
    Task<BrowserSession?> Find(string sessionId, CancellationToken cancellationToken);
}

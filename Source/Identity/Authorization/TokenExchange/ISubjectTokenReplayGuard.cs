// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Authorization.TokenExchange;

/// <summary>
/// Atomically remembers the first use of a verified subject token.
/// </summary>
internal interface ISubjectTokenReplayGuard
{
    /// <summary>
    /// Records a token only if it has not already been used within its acceptance window.
    /// </summary>
    /// <param name="key">The verified replay key.</param>
    /// <param name="until">The end of the acceptance window, including clock skew.</param>
    /// <param name="cancellationToken">The caller's cancellation token.</param>
    /// <returns>The recording outcome; failures never count as a successful record.</returns>
    Task<ReplayRecordResult> Record(string key, DateTimeOffset until, CancellationToken cancellationToken);
}

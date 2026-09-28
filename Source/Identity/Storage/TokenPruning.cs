// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;

namespace Cratis.Identity.Storage;

/// <summary>
/// Removes expired and revoked tokens and authorizations periodically.
/// </summary>
/// <remarks>
/// The threshold is the refresh-token lifetime, never shorter: reuse detection depends on the entry of a
/// redeemed refresh token still being present when a stolen copy is replayed, and a replayed token older than
/// that lifetime is rejected by its own expiry anyway.
/// </remarks>
/// <param name="scopes">The <see cref="IServiceScopeFactory"/>.</param>
/// <param name="options">The <see cref="IdentityOptions"/>.</param>
/// <param name="timeProvider">The <see cref="TimeProvider"/>.</param>
public class TokenPruning(IServiceScopeFactory scopes, IOptions<IdentityOptions> options, TimeProvider timeProvider) : BackgroundService
{
    static readonly TimeSpan _interval = TimeSpan.FromHours(1);

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_interval, timeProvider);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            using var scope = scopes.CreateScope();
            var threshold = timeProvider.GetUtcNow() - options.Value.Tokens.RefreshTokenLifetime;
            await scope.ServiceProvider.GetRequiredService<IOpenIddictTokenManager>().PruneAsync(threshold, stoppingToken);
            await scope.ServiceProvider.GetRequiredService<IOpenIddictAuthorizationManager>().PruneAsync(threshold, stoppingToken);
        }
    }
}

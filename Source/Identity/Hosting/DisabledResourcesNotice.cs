// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Hosting;

/// <summary>
/// Logs once at startup which optional resources are disabled because they are entirely unconfigured. Logs no configuration values.
/// </summary>
/// <param name="logger">The logger.</param>
/// <param name="audiences">The disabled audiences.</param>
internal sealed class DisabledResourcesNotice(ILogger<DisabledResourcesNotice> logger, IReadOnlyList<string> audiences) : IHostedService
{
    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var audience in audiences)
        {
            logger.OptionalResourceDisabled(audience);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

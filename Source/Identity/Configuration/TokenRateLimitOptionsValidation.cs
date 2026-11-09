// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Configuration;

/// <summary>Validates token endpoint admission settings without exposing configured values.</summary>
internal static class TokenRateLimitOptionsValidation
{
    /// <summary>Checks enabled limit ranges.</summary>
    /// <param name="options">The limit settings.</param>
    /// <returns>Value-free validation failures.</returns>
    internal static IEnumerable<string> Validate(TokenRateLimitOptions options)
    {
        if (!options.Enabled) yield break;

        if (options.Window <= TimeSpan.Zero || options.Window > TimeSpan.FromHours(1))
            yield return "TokenRateLimit:Window must be positive and no greater than one hour.";
        if (options.PerSourceRequests <= 0)
            yield return "TokenRateLimit:PerSourceRequests must be positive.";
        if (options.PerClientRequests <= 0)
            yield return "TokenRateLimit:PerClientRequests must be positive.";
        if (options.PerClientExchangeRequests <= 0)
            yield return "TokenRateLimit:PerClientExchangeRequests must be positive.";
        if (options.MaxTrackedKeys is < 100 or > 1_000_000)
            yield return "TokenRateLimit:MaxTrackedKeys must be between 100 and 1000000.";
    }
}

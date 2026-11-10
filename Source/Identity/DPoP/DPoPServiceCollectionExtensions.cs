// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.DPoP.Operational;

namespace Cratis.Identity.DPoP;

/// <summary>Registers opt-in DPoP admission and replay protection.</summary>
internal static class DPoPServiceCollectionExtensions
{
    /// <summary>Adds the DPoP services when enabled.</summary>
    /// <param name="services">The services.</param>
    /// <param name="enabled">Whether DPoP is enabled.</param>
    internal static void AddDPoP(this IServiceCollection services, bool enabled)
    {
        if (enabled)
        {
            services.AddScoped<DPoPBinding>();
            services.AddScoped<DPoPAdmission>();
            services.AddSingleton<DPoPProofValidator>();
            services.AddSingleton<DPoPNonce>();
            services.AddSingleton<IDPoPReplayStore, MongoDPoPReplayStore>();
        }
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.ConsentFlow.Operational;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cratis.Identity.ConsentFlow;

/// <summary>Registers the dormant, fail-closed consent state machine without changing the live endpoint.</summary>
internal static class ConsentFlowServiceCollectionExtensions
{
    /// <summary>Adds operational consent activation.</summary>
    /// <param name="services">The services.</param>
    /// <param name="configuration">The consent activation configuration.</param>
    internal static void AddConsentActivation(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ConsentActivationOptions>().Bind(configuration).ValidateDataAnnotations().ValidateOnStart();
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddScoped<IConsentRequestStore, MongoConsentRequestStore>();
        services.TryAddScoped<IConsentEvidence, UnavailableConsentServices>();
        services.TryAddScoped<IRevocationFence, UnavailableConsentServices>();
        services.TryAddScoped<IAuthorizationActivator, UnavailableConsentServices>();
        services.TryAddScoped<ICurrentConsentSession, UnavailableConsentSession>();
        services.TryAddScoped<IConsentEligibility, ConsentEligibilityReader>();
        services.TryAddSingleton<IConsentProof, ConsentProof>();
        services.TryAddScoped<ConsentCaller>();
        services.TryAddScoped<ConsentInteraction>();
        services.TryAddScoped<ConsentActivation>();
        services.AddHostedService<ConsentReconciler>();
    }
}

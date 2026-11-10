// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.ConsentFlow;
using Cratis.Identity.Contracts.Check;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Identity.for_IdentityServiceCollectionExtensions.given;

/// <summary>Separates runtime-owned implementation details from Identity's registration contract.</summary>
internal static class FrameworkRegistrationSnapshot
{
    /// <summary>
    /// .NET 11 removed antiforgery's pools and changed ValidateOnStart/ValidateDataAnnotations internals.
    /// These exact services are compared with isolated framework registrations, not silently omitted.
    /// </summary>
    static readonly string[] _runtimeServices =
    [
        "Microsoft.Extensions.ObjectPool.ObjectPoolProvider|",
        "Microsoft.Extensions.ObjectPool.ObjectPool`1[[Microsoft.AspNetCore.Antiforgery.AntiforgerySerializationContext,",
        "Microsoft.Extensions.Options.IStartupValidator|",
        "Microsoft.Extensions.Options.IAsyncStartupValidator|",
        "Microsoft.Extensions.Options.StartupValidator|",
        "Microsoft.Extensions.Options.IValidateOptions`1[[Cratis.Identity.ConsentFlow.ConsentActivationOptions,",
        "Microsoft.Extensions.DependencyInjection.DataAnnotationValidateOptionsAdapter`1[[Cratis.Identity.ConsentFlow.ConsentActivationOptions,",
        "Microsoft.Extensions.DependencyInjection.DataAnnotationValidateOptionsRegistration`1[[Cratis.Identity.ConsentFlow.ConsentActivationOptions,"
    ];

    internal static IServiceCollection Register()
    {
        var services = new ServiceCollection();
        services.AddAntiforgery();
        services.AddOptions<ConsentActivationOptions>().ValidateDataAnnotations().ValidateOnStart();
        return services;
    }

    internal static bool IsRuntimeOwned(string entry) =>
        _runtimeServices.Any(service => entry.StartsWith($"descriptor|{service}", StringComparison.Ordinal));

    internal static string[] IdentityRegistrations(IEnumerable<string> snapshot) =>
        snapshot.Where(entry => !IsRuntimeOwned(entry)).Select(FrameworkTypeIdentity.Canonicalize).ToArray();
}

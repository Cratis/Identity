// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Identity.Operational.for_LegacyAccountStoreRegistration;

public class when_registering_identity : Specification
{
    Type[] _identityHostedServices;

    void Because()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Development });
        builder.Services.AddSingleton(Substitute.For<IChronicleClient>());
        builder.AddCratisIdentity();
        _identityHostedServices = builder.Services
            .Where(service => service.ServiceType == typeof(IHostedService))
            .Select(service => service.ImplementationType)
            .OfType<Type>()
            .Where(type => type.Assembly == typeof(LegacyAccountStoreGuard).Assembly)
            .ToArray();
    }

    [Fact] void should_register_the_guard() => _identityHostedServices.ShouldContain(typeof(LegacyAccountStoreGuard));
    [Fact] void should_run_the_guard_before_any_other_identity_hosted_service() => _identityHostedServices[0].ShouldEqual(typeof(LegacyAccountStoreGuard));
}

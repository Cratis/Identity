// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Identity.DPoP;
using Cratis.Identity.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Identity.for_DPoP;

public class when_registering_the_disabled_profile : Specification
{
    WebApplicationBuilder _builder;

    void Because()
    {
        _builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Development });
        _builder.Configuration["Identity:DPoP:Enabled"] = "false";
        _builder.Services.AddSingleton(Substitute.For<IChronicleClient>());
        _builder.AddCratisIdentity();
    }

    [Fact] void should_not_register_any_dpop_service() => _builder.Services.Any(service => service.ServiceType.Namespace?.StartsWith(typeof(DPoPAdmission).Namespace, StringComparison.Ordinal) == true).ShouldBeFalse();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Identity.Accounts;
using Cratis.Identity.Hosting;
using Cratis.Identity.Upstream;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Identity.for_IdentityServiceCollectionExtensions;

public class when_registering_default_accounts : Specification
{
    WebApplicationBuilder _builder;

    void Because()
    {
        _builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Development });
        _builder.Services.AddSingleton(Substitute.For<IChronicleClient>());
        _builder.AddCratisIdentity();
    }

    [Fact] void should_register_the_native_writer() => _builder.Services.Any(service => service.ServiceType == typeof(EventSourcedAccountAdapter)).ShouldBeTrue();
    [Fact] void should_register_the_native_reader() => _builder.Services.Any(service => service.ServiceType == typeof(IAccountReader)).ShouldBeTrue();
    [Fact] void should_register_the_native_sign_in() => _builder.Services.Any(service => service.ServiceType == typeof(IAccountSignIn)).ShouldBeTrue();
}

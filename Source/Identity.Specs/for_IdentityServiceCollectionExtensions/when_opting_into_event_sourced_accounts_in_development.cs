// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts;
using Cratis.Identity.Accounts.Lookup;
using Cratis.Identity.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Identity.for_IdentityServiceCollectionExtensions;

public class when_opting_into_event_sourced_accounts_in_development : Specification
{
    WebApplicationBuilder _builder;
    ServiceProvider _provider;
    UpstreamLookupKey _key;

    void Because()
    {
        _builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Development });
        _builder.Configuration["Identity:Accounts:EventSourced"] = "true";
        _builder.AddCratisIdentity();
        _provider = _builder.Services.BuildServiceProvider();
        _key = _provider.GetRequiredService<IUpstreamLookupKeys>().Current;
    }

    [Fact] void should_register_the_opt_in_writer() => _builder.Services.Any(service => service.ServiceType == typeof(EventSourcedAccountAdapter)).ShouldBeTrue();
    [Fact] void should_not_replace_mongo_sign_in() => _builder.Services.Any(service => service.ServiceType == typeof(IAccounts)).ShouldBeTrue();
    [Fact] void should_supply_the_development_lookup_key() => _key.Revision.ShouldEqual(1);

    void Destroy() => _provider.Dispose();
}

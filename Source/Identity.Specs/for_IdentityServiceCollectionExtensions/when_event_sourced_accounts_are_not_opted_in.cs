// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts;
using Cratis.Identity.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace Cratis.Identity.for_IdentityServiceCollectionExtensions;

public class when_event_sourced_accounts_are_not_opted_in : Specification
{
    WebApplicationBuilder _builder;

    void Because()
    {
        _builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Development });
        _builder.AddCratisIdentity();
    }

    [Fact] void should_leave_the_new_writer_unregistered() => _builder.Services.Any(service => service.ServiceType == typeof(EventSourcedAccounts)).ShouldBeFalse();
    [Fact] void should_keep_the_mongo_account_service() => _builder.Services.Any(service => service.ServiceType == typeof(IAccounts)).ShouldBeTrue();
}

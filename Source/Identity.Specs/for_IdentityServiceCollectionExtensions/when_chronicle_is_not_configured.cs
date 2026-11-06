// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace Cratis.Identity.for_IdentityServiceCollectionExtensions;

public class when_chronicle_is_not_configured : Specification
{
    Exception _error;

    void Because()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Development });
        builder.Configuration["Identity:Accounts:EventSourced"] = "true";
        try
        {
            builder.AddCratisIdentity();
        }
        catch (ChronicleNotConfigured error)
        {
            _error = error;
        }
    }

    [Fact] void should_refuse_startup_without_native_chronicle() => _error.ShouldBeOfExactType<ChronicleNotConfigured>();
}

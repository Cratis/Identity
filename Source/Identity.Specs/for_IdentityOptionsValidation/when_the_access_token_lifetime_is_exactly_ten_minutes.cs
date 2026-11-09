// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Cratis.Identity.given;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.for_IdentityOptionsValidation;

public class when_the_access_token_lifetime_is_exactly_ten_minutes : Specification
{
    ValidateOptionsResult _result;

    void Because()
    {
        var validation = new IdentityOptionsValidation(new HostingEnvironment { EnvironmentName = Environments.Development });
        var options = Configurations.TwoResourceServers();
        options.Tokens.AccessTokenLifetime = TimeSpan.FromSeconds(600);
        _result = validation.Validate(null, options);
    }

    [Fact] void should_accept_it() => _result.Succeeded.ShouldBeTrue();
}

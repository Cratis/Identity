// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Cratis.Identity.given;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.for_IdentityOptionsValidation;

public class when_enabling_event_sourced_accounts_outside_development
{
    [Fact]
    void should_allow_the_opt_in_when_a_lookup_key_is_configured() => Validate(Convert.ToBase64String(new byte[32])).Succeeded.ShouldBeTrue();

    [Fact]
    void should_refuse_the_opt_in_when_the_lookup_key_is_missing() => Validate(string.Empty).Failures.ShouldContain("Event-sourced accounts outside Development require a configured account lookup key.");

    static ValidateOptionsResult Validate(string lookupKey)
    {
        var options = Configurations.CallbackResources();
        options.Accounts.EventSourced = true;
        options.Accounts.LookupKey = lookupKey;
        return new IdentityOptionsValidation(new HostingEnvironment { EnvironmentName = Environments.Production }).Validate(null, options);
    }
}

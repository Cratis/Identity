// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Identity.for_IdentityOptionsValidation;

public class when_the_id_token_lifetime_is_too_long : given.an_oidc_configuration
{
    ValidateOptionsResult _result;

    void Establish() => options.Tokens.IdentityTokenLifetime = TimeSpan.FromHours(1) + TimeSpan.FromSeconds(1);
    void Because() => _result = Validate();

    [Fact] void should_refuse_a_lifetime_above_one_hour() => _result.Failed.ShouldBeTrue();
    [Fact] void should_name_the_id_token_option() => _result.Failures.ShouldContain("Identity:Tokens:IdentityTokenLifetime must be positive and no greater than 01:00:00.");
}

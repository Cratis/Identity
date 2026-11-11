// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Identity.for_IdentityOptionsValidation;

public class when_the_id_token_lifetime_is_zero : given.an_oidc_configuration
{
    ValidateOptionsResult _result;

    void Establish() => options.Tokens.IdentityTokenLifetime = TimeSpan.Zero;
    void Because() => _result = Validate();

    [Fact] void should_refuse_a_non_positive_lifetime() => _result.Failed.ShouldBeTrue();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Cratis.Identity.given;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.for_IdentityOptionsValidation;

public class when_validating_hostile_client_options
{
    [Fact] void should_require_a_key_in_production() => Validate(new() { EventSourced = true }, Environments.Production).Failures.ShouldContain("A client secret digest key is required outside Development.");
    [Fact] void should_allow_the_default_off_in_production() => Validate(new(), Environments.Production).Succeeded.ShouldBeTrue();
    [Fact] void should_allow_a_production_key() => Validate(new() { EventSourced = true, SecretDigestKey = Convert.ToBase64String(new byte[32]) }, Environments.Production).Succeeded.ShouldBeTrue();
    [Fact] void should_allow_a_development_fallback() => Validate(new() { EventSourced = true }).Succeeded.ShouldBeTrue();
    [Fact] void should_refuse_revision_zero() => Validate(new() { SecretDigestKeyRevision = 0 }).Failures.ShouldContain("Client secret digest key revision must be positive.");

    [Theory]
    [InlineData("AA==")]
    [InlineData("not-base64!")]
    void should_refuse_unsafe_keys_even_with_the_flag_off(string key) => Validate(new() { SecretDigestKey = key }).Failures.ShouldContain("Client secret digest key must be base64 with at least 256 bits of key material.");

    static ValidateOptionsResult Validate(ClientsOptions clients, string environment = "Development")
    {
        var options = Configurations.CallbackResources();
        options.Clients = clients;
        return new IdentityOptionsValidation(new HostingEnvironment { EnvironmentName = environment }).Validate(null, options);
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Cratis.Identity.given;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.for_IdentityOptionsValidation;

public class when_validating_hostile_account_options
{
    [Theory]
    [InlineData("not-base64!")]
    [InlineData("AA==")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==")]
    void should_refuse_malformed_or_short_key_material(string key) => Validate(new() { EventSourced = true, LookupKey = key }).Failures.ShouldContain("Account lookup key must be base64 with at least 256 bits of key material.");

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    void should_refuse_invalid_revisions(int revision) => Validate(new() { EventSourced = true, LookupKeyRevision = revision }).Failures.ShouldContain("Account lookup revision and link hold must be valid.");

    [Fact]
    void should_refuse_a_negative_hold() => Validate(new() { EventSourced = true, LinkHoldMinimum = TimeSpan.FromTicks(-1) }).Failures.ShouldContain("Account lookup revision and link hold must be valid.");

    [Fact]
    void should_accept_explicit_minimum_key_material() => Validate(new() { EventSourced = true, LookupKey = Convert.ToBase64String(new byte[32]) }).Succeeded.ShouldBeTrue();

    static ValidateOptionsResult Validate(AccountsOptions accounts)
    {
        var options = Configurations.TwoResourceServers();
        options.Accounts = accounts;
        return new IdentityOptionsValidation(new HostingEnvironment { EnvironmentName = Environments.Development }).Validate(null, options);
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Cratis.Identity.given;

namespace Cratis.Identity.for_TokenExchangeOptionsValidation;

public class when_skew_or_age_or_timeout_is_out_of_range : Specification
{
    Action<ExternalIssuerOptions>[] _mutations =
    [
        issuer => issuer.ClockSkew = TimeSpan.FromSeconds(121),
        issuer => issuer.ClockSkew = TimeSpan.FromSeconds(-1),
        issuer => issuer.MaxTokenAge = TimeSpan.Zero,
        issuer => issuer.MaxTokenAge = TimeSpan.FromHours(1) + TimeSpan.FromSeconds(1),
        issuer => issuer.JwksTimeout = TimeSpan.Zero,
        issuer => issuer.JwksTimeout = TimeSpan.FromSeconds(11)
    ];

    bool[] _failed;
    bool _limitsSucceeded;

    void Because()
    {
        _failed = [.. _mutations.Select(mutate =>
        {
            var options = Configurations.WithTokenExchange();
            mutate(options.TokenExchange.Issuers[0]);
            return Validate(options).Failed;
        })];

        var limits = Configurations.WithTokenExchange();
        limits.TokenExchange.Issuers[0].ClockSkew = TimeSpan.FromSeconds(120);
        limits.TokenExchange.Issuers[0].MaxTokenAge = TimeSpan.FromHours(1);
        limits.TokenExchange.Issuers[0].JwksTimeout = TimeSpan.FromSeconds(10);
        _limitsSucceeded = Validate(limits).Succeeded;
    }

    static Microsoft.Extensions.Options.ValidateOptionsResult Validate(IdentityOptions options) =>
        new IdentityOptionsValidation(new Microsoft.Extensions.Hosting.Internal.HostingEnvironment { EnvironmentName = "Production" }).Validate(null, options);

    [Fact] void should_refuse_every_out_of_range_value() => _failed.All(_ => _).ShouldBeTrue();
    [Fact] void should_accept_the_limits_themselves() => _limitsSucceeded.ShouldBeTrue();
}

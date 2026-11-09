// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cratis.Identity.for_TokenRateLimitOptionsValidation;

public class when_validating_limits : Specification
{
    string[] _defaults;
    string[] _invalid;
    string[] _disabled;
    string[] _negative;
    string[] _excessive;
    IdentityOptions _bound;

    void Because()
    {
        _defaults = TokenRateLimitOptionsValidation.Validate(new()).ToArray();
        var options = new TokenRateLimitOptions { Window = TimeSpan.Zero, PerSourceRequests = 0, PerClientRequests = 0, PerClientExchangeRequests = 0, MaxTrackedKeys = 99 };
        _invalid = TokenRateLimitOptionsValidation.Validate(options).ToArray();
        options.Enabled = false;
        _disabled = TokenRateLimitOptionsValidation.Validate(options).ToArray();
        _negative = TokenRateLimitOptionsValidation.Validate(new() { Window = TimeSpan.FromSeconds(-1), PerSourceRequests = -1, PerClientRequests = -1, PerClientExchangeRequests = -1, MaxTrackedKeys = -1 }).ToArray();
        _excessive = TokenRateLimitOptionsValidation.Validate(new() { Window = TimeSpan.FromHours(2), MaxTrackedKeys = 1_000_001 }).ToArray();
        _bound = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Identity:TokenRateLimit:PerSourceRequests"] = "42" }).Build().GetSection("Identity").Get<IdentityOptions>();
    }

    [Fact] void should_accept_defaults() => _defaults.ShouldBeEmpty();
    [Fact] void should_reject_each_invalid_setting() => _invalid.Length.ShouldEqual(5);
    [Fact] void should_skip_disabled_validation() => _disabled.ShouldBeEmpty();
    [Fact] void should_reject_negative_settings() => _negative.Length.ShouldEqual(5);
    [Fact] void should_bound_window_and_capacity() => _excessive.Length.ShouldEqual(2);
    [Fact] void should_bind_the_identity_section() => _bound.TokenRateLimit.PerSourceRequests.ShouldEqual(42);
}

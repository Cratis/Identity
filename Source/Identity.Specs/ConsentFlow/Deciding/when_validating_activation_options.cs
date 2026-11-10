// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Cratis.Identity.ConsentFlow.Deciding;

public class when_validating_activation_options : Specification
{
    bool[] _invalid;
    bool _defaults;
    readonly ConsentActivationOptions _options = new();

    void Because()
    {
        _defaults = Validator.TryValidateObject(_options, new ValidationContext(_options), [], true);
        var invalid = new[]
        {
            new ConsentActivationOptions { ActivationDeadline = TimeSpan.Zero },
            new ConsentActivationOptions { TerminalRetention = TimeSpan.Zero },
            new ConsentActivationOptions { BatchSize = 0 },
            new ConsentActivationOptions { ReconcileInterval = TimeSpan.Zero }
        };
        _invalid = invalid.Select(_ => Validator.TryValidateObject(_, new ValidationContext(_), [], true)).ToArray();
    }

    [Fact] void should_accept_the_defaults() => _defaults.ShouldBeTrue();
    [Fact] void should_default_to_ten_minutes() => _options.ActivationDeadline.ShouldEqual(TimeSpan.FromMinutes(10));
    [Fact] void should_default_to_thirty_days_retention() => _options.TerminalRetention.ShouldEqual(TimeSpan.FromDays(30));
    [Fact] void should_reject_invalid_settings() => _invalid.All(_ => !_).ShouldBeTrue();
}

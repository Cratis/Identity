// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Clients.Registering;
using Cratis.Identity.Clients.RotatingSecret;

namespace Cratis.Identity.Clients;

public class when_bounding_secret_rotations : given.client_commands
{
    [Theory]
    [InlineData(-25, true)]
    [InlineData(-24, false)]
    [InlineData(24, false)]
    [InlineData(25, true)]
    void should_bound_activation_by_the_server_clock(int hours, bool refused)
    {
        var activation = Now.AddHours(hours);
        var state = _active with { ActivatedAt = Now.AddDays(-2) };
        var command = new RotateSecret(_id, Plaintext, new(activation, activation));
        command.Handle(state, _digester, _clock, _secretOptions).TryGetError(out _).ShouldEqual(refused);
    }

    [Theory]
    [InlineData(7, false)]
    [InlineData(8, true)]
    void should_cap_overlap_at_seven_days_by_default(int days, bool refused)
    {
        var command = new RotateSecret(_id, Plaintext, new(Now, Now.AddDays(days)));
        command.Handle(_active, _digester, _clock, _secretOptions).TryGetError(out _).ShouldEqual(refused);
    }

    [Fact]
    void should_apply_the_configured_overlap_cap()
    {
        _secretOptions.Value.MaxOverlap = TimeSpan.FromHours(2);
        new RotateSecret(_id, Plaintext, new(Now, Now.AddHours(2))).Handle(_active, _digester, _clock, _secretOptions).TryGetError(out _).ShouldBeFalse();
        new RotateSecret(_id, Plaintext, new(Now, Now.AddHours(2).AddTicks(1))).Handle(_active, _digester, _clock, _secretOptions).TryGetError(out _).ShouldBeTrue();
    }

    [Fact]
    void should_refuse_extreme_dates_without_overflowing()
    {
        new RotateSecret(_id, Plaintext, new(DateTimeOffset.MaxValue, DateTimeOffset.MaxValue)).Handle(_active, _digester, _clock, _secretOptions).TryGetError(out _).ShouldBeTrue();
        new RotateSecret(_id, Plaintext, new(Now, DateTimeOffset.MaxValue)).Handle(_active, _digester, _clock, _secretOptions).TryGetError(out _).ShouldBeTrue();
        new RotateSecret(_id, Plaintext, new(DateTimeOffset.MinValue, Now)).Handle(_active, _digester, _clock, _secretOptions).TryGetError(out _).ShouldBeTrue();
    }

    [Fact]
    void should_refuse_rotation_to_the_current_digest()
    {
        var state = _active with { SecretHash = _digester.Digest(Plaintext) };
        new RotateSecret(_id, Plaintext, new(Now, Now)).Handle(state, _digester, _clock, _secretOptions).TryGetError(out _).ShouldBeTrue();
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(15, true)]
    [InlineData(16, false)]
    void should_require_at_least_sixteen_characters_on_registration_and_rotation(int length, bool refused)
    {
        var secret = new string('s', length);
        new RegisterClient(new("strength-spec"), _configuration, secret).Handle(_registrationDecision, _digester).TryGetError(out _).ShouldEqual(refused);
        new RotateSecret(_id, secret, new(Now, Now)).Handle(_active, _digester, _clock, _secretOptions).TryGetError(out _).ShouldEqual(refused);
    }

    [Fact]
    void should_refuse_whitespace_only_secrets_even_at_the_minimum_length()
    {
        var secret = new string(' ', 16);
        new RegisterClient(new("strength-spec"), _configuration, secret).Handle(_registrationDecision, _digester).TryGetError(out _).ShouldBeTrue();
        new RotateSecret(_id, secret, new(Now, Now)).Handle(_active, _digester, _clock, _secretOptions).TryGetError(out _).ShouldBeTrue();
    }
}

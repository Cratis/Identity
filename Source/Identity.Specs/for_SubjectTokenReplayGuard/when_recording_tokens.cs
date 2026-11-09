// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization.TokenExchange;
using Cratis.Identity.given;

namespace Cratis.Identity.for_SubjectTokenReplayGuard;

public class when_recording_tokens : Specification
{
    FixedTimeProvider _time;
    SubjectTokenReplayGuard _guard;
    bool _first;
    bool _second;
    bool _usedBeforeExpiry;
    bool _usedAfterExpiry;
    bool _afterExpiry;

    void Establish()
    {
        _time = new(DateTimeOffset.UtcNow);
        _guard = new(_time);
    }

    void Because()
    {
        _first = _guard.TryRecord("a", _time.Now.AddMinutes(1));
        _second = _guard.TryRecord("a", _time.Now.AddMinutes(1));
        _usedBeforeExpiry = _guard.WasUsed("a");
        _time.Now = _time.Now.AddMinutes(2);
        _usedAfterExpiry = _guard.WasUsed("a");
        _afterExpiry = _guard.TryRecord("a", _time.Now.AddMinutes(1));
    }

    [Fact] void should_record_the_first_use() => _first.ShouldBeTrue();
    [Fact] void should_refuse_the_second_use() => _second.ShouldBeFalse();
    [Fact] void should_remember_it_until_expiry() => _usedBeforeExpiry.ShouldBeTrue();
    [Fact] void should_forget_it_after_expiry() => _usedAfterExpiry.ShouldBeFalse();
    [Fact] void should_record_it_again_after_expiry() => _afterExpiry.ShouldBeTrue();
}

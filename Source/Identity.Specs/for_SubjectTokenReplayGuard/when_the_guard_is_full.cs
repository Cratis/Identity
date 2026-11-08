// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization.TokenExchange;
using Cratis.Identity.given;

namespace Cratis.Identity.for_SubjectTokenReplayGuard;

public class when_the_guard_is_full : Specification
{
    FixedTimeProvider _time;
    SubjectTokenReplayGuard _guard;
    bool _refusedWhenFull;
    bool _firstStillRemembered;
    bool _acceptedAfterExpiry;

    void Establish()
    {
        _time = new(DateTimeOffset.UtcNow);
        _guard = new(_time);
    }

    void Because()
    {
        for (var i = 0; i < SubjectTokenReplayGuard.MaximumEntries; i++)
        {
            _guard.TryRecord($"k{i}", _time.Now.AddMinutes(1));
        }

        _refusedWhenFull = !_guard.TryRecord("one-too-many", _time.Now.AddMinutes(1));
        _firstStillRemembered = _guard.WasUsed("k0");
        _time.Now = _time.Now.AddMinutes(2);
        _acceptedAfterExpiry = _guard.TryRecord("one-too-many", _time.Now.AddMinutes(1));
    }

    [Fact] void should_refuse_rather_than_forget() => _refusedWhenFull.ShouldBeTrue();
    [Fact] void should_not_evict_a_live_entry() => _firstStillRemembered.ShouldBeTrue();
    [Fact] void should_make_room_by_dropping_only_expired_entries() => _acceptedAfterExpiry.ShouldBeTrue();
}

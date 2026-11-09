// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization.TokenExchange;

namespace Cratis.Identity.for_SubjectTokenValidator;

public class when_validating_a_token_that_was_already_used : given.a_subject_token_validator
{
    SubjectTokenResult _first;
    SubjectTokenRefusal _second;
    SubjectTokenRefusal _anotherJti;
    SubjectTokenRefusal _afterExpiry;
    bool _recordedTwice;

    async Task Because()
    {
        var token = Valid();
        _first = await Validate(token);
        replay.TryRecord(_first.Subject.ReplayKey, _first.Subject.Expiry.AddSeconds(60));
        _recordedTwice = replay.TryRecord(_first.Subject.ReplayKey, _first.Subject.Expiry.AddSeconds(60));
        _second = await RefusalOf(token);
        _anotherJti = await RefusalOf(Valid());
        time.Now = time.Now.AddMinutes(10);
        _afterExpiry = (await Validate(token)).Refusal;
    }

    [Fact] void should_accept_the_first_use() => _first.Refusal.ShouldEqual(SubjectTokenRefusal.None);
    [Fact] void should_refuse_recording_it_twice() => _recordedTwice.ShouldBeFalse();
    [Fact] void should_leave_replay_refusal_to_the_atomic_grant_check() => _second.ShouldEqual(SubjectTokenRefusal.None);
    [Fact] void should_accept_a_token_with_another_jti() => _anotherJti.ShouldEqual(SubjectTokenRefusal.None);
    [Fact] void should_not_report_replay_once_the_token_has_expired() => _afterExpiry.ShouldNotEqual(SubjectTokenRefusal.Replayed);
}

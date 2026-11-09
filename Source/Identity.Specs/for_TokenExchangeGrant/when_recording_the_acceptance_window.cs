// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization.TokenExchange;

namespace Cratis.Identity.for_TokenExchangeGrant;

public class when_recording_the_acceptance_window : given.a_token_exchange_request
{
    readonly List<DateTimeOffset> _deadlines = [];
    DateTimeOffset _shortDeadline;
    DateTimeOffset _longDeadline;

    void Establish()
    {
        _replayGuard = Substitute.For<ISubjectTokenReplayGuard>();
        _replayGuard.Record(Arg.Any<string>(), Arg.Do<DateTimeOffset>(_deadlines.Add), Arg.Any<CancellationToken>()).Returns(ReplayRecordResult.Recorded);
    }

    async Task Because()
    {
        var issuer = _options.TokenExchange.Issuers[0];
        _shortDeadline = _subject.Expiry + issuer.ClockSkew;
        await Handle();
        _subject = _subject with { Expiry = _time.Now.AddHours(1) };
        _validator.Validate(default!, default!, TestContext.Current.CancellationToken).ReturnsForAnyArgs(Task.FromResult(SubjectTokenResult.Accepted(_subject)));
        _longDeadline = _subject.IssuedAt + issuer.MaxTokenAge + issuer.ClockSkew;
        await Handle();
    }

    [Fact] void should_keep_a_short_lived_token_through_expiry_and_skew() => _deadlines[0].ShouldEqual(_shortDeadline);
    [Fact] void should_keep_a_long_lived_token_through_maximum_age_and_skew() => _deadlines[1].ShouldEqual(_longDeadline);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization.TokenExchange;

namespace Cratis.Identity.for_TokenExchangeGrant;

public class when_the_replay_store_is_unavailable : given.a_token_exchange_request
{
    void Establish()
    {
        _replayGuard = Substitute.For<ISubjectTokenReplayGuard>();
        _replayGuard.Record(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(ReplayRecordResult.Unavailable);
    }

    async Task Because() => await Handle();

    [Fact] void should_report_temporary_unavailability() => Error.ShouldEqual("temporarily_unavailable");
    [Fact] void should_not_issue_a_token() => IsError.ShouldBeTrue();
    [Fact] void should_preserve_the_existing_retry_description() => Description.ShouldEqual("Token exchange is unavailable right now. Try again later.");
    [Fact] void should_log_a_stable_outage_category() => _logger.Messages.ShouldContain(_ => _.Contains("ReplayGuardUnavailable") && _.Contains("replay-guard-unavailable"));
    [Fact] void should_not_log_the_subject_token() => _logger.Messages.ShouldNotContain(_ => _.Contains("subject-token-placeholder"));
    [Fact] void should_not_log_the_verified_subject_or_replay_key() => _logger.Messages.ShouldNotContain(_ => _.Contains(_subject.Subject.Value) || _.Contains(_subject.ReplayKey));
}

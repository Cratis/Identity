// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts.Lookup;
using Cratis.Identity.Authorization.TokenExchange;

namespace Cratis.Identity.for_TokenExchangeGrant;

public class when_checks_fail_before_recording : given.a_token_exchange_request
{
    void Establish() => _replayGuard = Substitute.For<ISubjectTokenReplayGuard>();

    async Task Because()
    {
        _validator.Validate(default!, default!, TestContext.Current.CancellationToken).ReturnsForAnyArgs(Task.FromResult(SubjectTokenResult.Refused(SubjectTokenRefusal.Invalid)));
        await Handle();
        _validator.Validate(default!, default!, TestContext.Current.CancellationToken).ReturnsForAnyArgs(Task.FromResult(SubjectTokenResult.Accepted(_subject)));
        _keys.Find(default!, default!).ReturnsForAnyArgs(Task.FromResult<UpstreamKeyMatch?>(null));
        await Handle();
        _keys.Find(default!, default!).ReturnsForAnyArgs(Task.FromResult<UpstreamKeyMatch?>(new UpstreamKeyMatch(_account.Id, default!, true)));
        await Run(_ => _.SetParameter(TokenExchangeConstants.Tenant, "other"));
    }

    [Fact]
    async Task should_not_consume_an_invalid_unlinked_or_unselectable_subject_token() =>
        await _replayGuard.DidNotReceive().Record(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Upstream;

namespace Cratis.Identity.Accounts.Linking.for_AccountSession;

public class when_requiring_proof : Specification
{
    const string Session = "session";
    const string Refusal = "fresh_session_bound_upstream_proof_required";
    static readonly DateTimeOffset _now = DateTimeOffset.FromUnixTimeSeconds(1800000000);

    ValidationResult? _exactlyFiveMinutes;
    ValidationResult? _oneTickOverFiveMinutes;
    ValidationResult? _future;
    ValidationResult? _otherSession;
    ValidationResult? _missing;

    void Because()
    {
        _exactlyFiveMinutes = AccountSession.RequireProof(Proof(Session, _now - TimeSpan.FromMinutes(5)), Session, _now);
        _oneTickOverFiveMinutes = AccountSession.RequireProof(Proof(Session, _now - TimeSpan.FromMinutes(5) - TimeSpan.FromTicks(1)), Session, _now);
        _future = AccountSession.RequireProof(Proof(Session, _now + TimeSpan.FromTicks(1)), Session, _now);
        _otherSession = AccountSession.RequireProof(Proof("other", _now), Session, _now);
        _missing = AccountSession.RequireProof(null, Session, _now);
    }

    [Fact] void should_accept_proof_exactly_five_minutes_old() => _exactlyFiveMinutes.ShouldBeNull();
    [Fact] void should_refuse_proof_one_tick_older_than_five_minutes() => _oneTickOverFiveMinutes!.Message.ShouldEqual(Refusal);
    [Fact] void should_refuse_proof_from_the_future() => _future!.Message.ShouldEqual(Refusal);
    [Fact] void should_refuse_proof_bound_to_another_session() => _otherSession!.Message.ShouldEqual(Refusal);
    [Fact] void should_refuse_missing_proof() => _missing!.Message.ShouldEqual(Refusal);

    static VerifiedAuthentication Proof(string session, DateTimeOffset authTime) =>
        new(new ConnectionId(Guid.NewGuid()), "https://github.com", "42", authTime, FactId.New()) { SessionId = session };
}

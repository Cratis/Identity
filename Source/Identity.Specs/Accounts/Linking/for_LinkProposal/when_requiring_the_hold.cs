// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Upstream;

namespace Cratis.Identity.Accounts.Linking.for_LinkProposal;

public class when_requiring_the_hold : Specification
{
    static readonly DateTimeOffset _proposedAt = DateTimeOffset.FromUnixTimeSeconds(1800000000);
    static readonly TimeSpan _minimum = TimeSpan.FromMinutes(10);
    static readonly TimeSpan _tick = TimeSpan.FromTicks(1);

    LinkProposal _proposal;
    LinkProposal _notBeforeLater;
    ValidationResult? _atBoundary;
    ValidationResult? _nowBeforeBoundary;
    ValidationResult? _proofBeforeBoundary;
    ValidationResult? _shorterThanNotBefore;
    ValidationResult? _notBeforeBoundaryReached;

    void Establish()
    {
        var connection = new ConnectionId(Guid.NewGuid());
        _proposal = new(Guid.NewGuid(), connection, "https://github.com", "42", _proposedAt, _proposedAt, "pending_proof");
        _notBeforeLater = _proposal with { HoldNotBefore = _proposedAt + TimeSpan.FromHours(1) };
    }

    void Because()
    {
        var boundary = _proposedAt + _minimum;
        _atBoundary = LinkProposal.RequireHold(_proposal, Proof(boundary), _minimum, boundary);
        _nowBeforeBoundary = LinkProposal.RequireHold(_proposal, Proof(boundary), _minimum, boundary - _tick);
        _proofBeforeBoundary = LinkProposal.RequireHold(_proposal, Proof(boundary - _tick), _minimum, boundary);
        var notBefore = _notBeforeLater.HoldNotBefore;
        _shorterThanNotBefore = LinkProposal.RequireHold(_notBeforeLater, Proof(notBefore), _minimum, notBefore - _tick);
        _notBeforeBoundaryReached = LinkProposal.RequireHold(_notBeforeLater, Proof(notBefore), _minimum, notBefore);
    }

    [Fact] void should_accept_when_now_and_proof_are_exactly_at_the_boundary() => _atBoundary.ShouldBeNull();
    [Fact] void should_refuse_one_tick_before_the_boundary() => _nowBeforeBoundary!.Message.ShouldEqual("realm_configured_hold_not_elapsed");
    [Fact] void should_refuse_proof_issued_one_tick_before_the_boundary() => _proofBeforeBoundary!.Message.ShouldEqual("fresh_post_hold_proof_required");
    [Fact] void should_use_the_hold_not_before_when_it_is_later_than_the_minimum_hold() => _shorterThanNotBefore!.Message.ShouldEqual("realm_configured_hold_not_elapsed");
    [Fact] void should_accept_at_the_hold_not_before_boundary() => _notBeforeBoundaryReached.ShouldBeNull();

    VerifiedAuthentication Proof(DateTimeOffset authTime) => new(_proposal.Connection, _proposal.Issuer, _proposal.Subject, authTime, FactId.New());
}

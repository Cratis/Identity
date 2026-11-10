// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Accounts.Linking.Confirming.when_confirming_a_link;

public class and_security_checks_fail_closed : given.a_confirmation_decision
{
    [Theory]
    [InlineData(false, "connection")]
    [InlineData(false, "issuer")]
    [InlineData(false, "subject")]
    [InlineData(true, "connection")]
    [InlineData(true, "issuer")]
    [InlineData(true, "subject")]
    void should_match_the_complete_upstream_tuple(bool clearing, string component)
    {
        SetProposal(component switch
        {
            "connection" => _proposal with { Connection = Guid.NewGuid() },
            "issuer" => _proposal with { Issuer = "https://different.example.test" },
            _ => _proposal with { Subject = "different-subject" }
        });
        Refused(Handle(clearing), "upstream_proof_mismatch");
    }

    [Theory]
    [InlineData(false, "confirmed", "proposal_not_confirmable")]
    [InlineData(false, "rejected", "proposal_not_confirmable")]
    [InlineData(false, "quarantined", "proposal_not_confirmable")]
    [InlineData(true, "confirmed", "quarantine_not_clearable")]
    [InlineData(true, "rejected", "quarantine_not_clearable")]
    [InlineData(true, "pending_proof", "quarantine_not_clearable")]
    void should_preserve_terminal_and_quarantine_rules(bool clearing, string state, string reason)
    {
        SetState(state);
        Refused(Handle(clearing), reason);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_refuse_an_unknown_proposal(bool clearing)
    {
        _decision = _decision with { State = _state with { Proposals = new Dictionary<Guid, LinkProposal>() } };
        Refused(Handle(clearing), "proposal_not_found");
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    [InlineData(true, false, true)]
    void should_require_an_active_account(bool clearing, bool disabled, bool tombstoned)
    {
        _decision = _decision with { State = _state with { Registered = disabled || tombstoned, Disabled = disabled, Tombstoned = tombstoned } };
        Refused(Handle(clearing), "account_not_active");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_enforce_the_current_realm_hold(bool clearing)
    {
        SetState(clearing ? "quarantined" : "pending_proof");
        _decision = _decision with { Hold = TimeSpan.FromMinutes(2) };
        Refused(Handle(clearing), "realm_configured_hold_not_elapsed");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_require_fresh_post_realm_hold_proof(bool clearing)
    {
        SetState(clearing ? "quarantined" : "pending_proof");
        _decision = _decision with { Hold = TimeSpan.FromMinutes(2), At = _clock.Now + TimeSpan.FromMinutes(2) };
        Refused(Handle(clearing), "fresh_post_hold_proof_required");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_preserve_proposal_hold_rejections(bool clearing)
    {
        SetState(clearing ? "quarantined" : "pending_proof");
        SetProposal(_proposal with { HoldNotBefore = _clock.Now + TimeSpan.FromSeconds(1) });
        Refused(Handle(clearing), clearing ? "quarantine_not_clearable" : "proposal_not_confirmable");
    }

    [Theory]
    [InlineData(true)]
    void should_refuse_clearance_with_pre_quarantine_boundary_proof(bool clearing)
    {
        SetState("quarantined");
        SetProposal(_proposal with { HoldNotBefore = _clock.Now + TimeSpan.FromSeconds(1) });
        _decision = _decision with { At = _clock.Now + TimeSpan.FromSeconds(2) };
        Refused(Handle(clearing), "quarantine_not_clearable");
    }
}

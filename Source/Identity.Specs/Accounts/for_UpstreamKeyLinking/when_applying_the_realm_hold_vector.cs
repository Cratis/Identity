// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.for_UpstreamKeyLinking;

public class when_applying_the_realm_hold_vector : given.a_contract_vector
{
    AccountSubject _account;
    LinkProposalId _proposal;
    AccountOperationRefused _refusal;

    async Task Establish()
    {
        await Load("realm-configured-hold-enforced");
        _options.Accounts.LinkHoldMinimum = TimeSpan.FromSeconds(_given.GetProperty("realm_policy").GetProperty("confirmation_hold_seconds").GetInt32());
        _account = new(Text(_given.GetProperty("accounts")[0], "subject"));
    }

    async Task Because()
    {
        var flow = _given.GetProperty("proposal");
        _clock.Now = DateTimeOffset.FromUnixTimeSeconds(flow.GetProperty("complete_proof_recorded_at").GetInt64());
        var proof = Verified(new(Text(flow.GetProperty("candidate_key"), "idp")), new(Text(flow.GetProperty("candidate_key"), "idp_sub")), FactId.New(), Session(_account));
        _proposal = await _linking.Propose(Session(_account), proof, new(true));
        await Refresh();
        _clock.Now = DateTimeOffset.FromUnixTimeSeconds(_when.GetProperty("now").GetInt64());
        try
        {
            await _linking.Confirm(Session(_account), _proposal, Verified(proof.IdpIssuer, proof.Subject, FactId.New(), Session(_account)));
        }
        catch (AccountOperationRefused exception)
        {
            _refusal = exception;
        }

        await Refresh();
    }

    [Fact] void should_keep_the_proposal_pending() => _states.InstanceForEventSourceId(_account.Value).Proposals[_proposal.Value].State.ShouldEqual(Text(_then, "proposal_state"));
    [Fact] void should_refuse_early_confirmation() => _refusal.Message.ShouldEqual(Text(_then, "reason"));
    [Fact] void should_not_grant_the_candidate_key() => _states.InstanceForEventSourceId(_account.Value).Links.Count.ShouldEqual(1);
}

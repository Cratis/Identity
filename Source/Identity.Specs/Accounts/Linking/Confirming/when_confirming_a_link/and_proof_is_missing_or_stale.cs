// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Upstream;

namespace Cratis.Identity.Accounts.Linking.Confirming.when_confirming_a_link;

public class and_proof_is_missing_or_stale : given.a_confirmation_decision
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    async Task should_require_upstream_proof(bool clearing)
    {
        Observe(null);
        Refused(await Provide(clearing), "fresh_session_bound_upstream_proof_required");
        _order.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false, null)]
    [InlineData(false, "foreign-session")]
    [InlineData(true, null)]
    [InlineData(true, "foreign-session")]
    async Task should_require_owner_session_binding(bool clearing, string? sessionId)
    {
        var proof = _ceremony.Authentication;
        var unbound = new VerifiedAuthentication(proof.ConnectionId, proof.IdpIssuer, proof.Subject, proof.AuthTime, proof.OperationId) { SessionId = sessionId };
        Observe(unbound);
        Refused(await Provide(clearing), "fresh_session_bound_upstream_proof_required");
        _order.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false, -1)]
    [InlineData(false, 301)]
    [InlineData(true, -1)]
    [InlineData(true, 301)]
    async Task should_refuse_future_or_stale_proof(bool clearing, int ageSeconds)
    {
        var proof = _ceremony.Authentication;
        Observe(new(proof.ConnectionId, proof.IdpIssuer, proof.Subject, _clock.Now - TimeSpan.FromSeconds(ageSeconds), proof.OperationId) { SessionId = proof.SessionId });
        Refused(await Provide(clearing), "fresh_session_bound_upstream_proof_required");
        _order.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false, -1)]
    [InlineData(false, 301)]
    [InlineData(true, -1)]
    [InlineData(true, 301)]
    async Task should_require_a_fresh_existing_account_session(bool clearing, int ageSeconds)
    {
        _clock.Now += TimeSpan.FromSeconds(ageSeconds);
        Refused(await Provide(clearing), "fresh_existing_account_proof_required");
        _order.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    async Task should_accept_proof_at_exactly_five_minutes(bool clearing)
    {
        _clock.Now += TimeSpan.FromMinutes(5);
        (await Provide(clearing)).TryGetError(out _).ShouldBeFalse();
    }
}

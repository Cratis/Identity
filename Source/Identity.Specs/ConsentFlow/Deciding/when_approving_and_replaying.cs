// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.ConsentFlow.Deciding;

public class when_approving_and_replaying : Pending.given.a_consent_interaction
{
    ConsentRequest? _decided;
    Exception? _replayed;
    Exception? _denied;
    string? _code;
    Pending.PendingConsent? _pending;

    async Task Because()
    {
        await Approve();
        _decided = await store.Read(request.Id, CancellationToken.None);
        _replayed = await Record.ExceptionAsync(() => Approve());
        _denied = await Record.ExceptionAsync(() => Deny());
        _code = await activation.ClaimCode(request.Id, binding, CancellationToken.None);
        _pending = await interaction.Find(CancellationToken.None);
    }

    [Fact] void should_only_decide() => _decided.State.ShouldEqual(ConsentRequestState.Decided);
    [Fact] void should_record_the_exact_canonical_selection() => _decided.Selection.ShouldEqual(new ConsentSelection(binding.Tenant, "openid read"));
    [Fact] void should_preserve_the_original_binding() => _decided.Binding.ShouldEqual(binding);
    [Fact] void should_refuse_replayed_approval() => _replayed.ShouldBeOfExactType<ConsentDecisionRefused>();
    [Fact] void should_refuse_denial_after_approval() => _denied.ShouldBeOfExactType<ConsentDecisionRefused>();
    [Fact] void should_never_write_evidence() => evidence.Appends.ShouldEqual(0);
    [Fact] void should_never_prepare_authorization() => authorizations.Count.ShouldEqual(0);
    [Fact] void should_not_have_an_authorization_identity() => _decided.AuthorizationId.ShouldBeNull();
    [Fact] void should_refuse_code_claims() => _code.ShouldBeNull();
    [Fact] void should_not_display_decided_requests() => _pending.ShouldBeNull();
    [Fact] void should_never_issue_code() => _decided.CodeClaimed.ShouldBeFalse();
}

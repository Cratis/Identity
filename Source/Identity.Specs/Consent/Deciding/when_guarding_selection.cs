// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.ConsentFlow.Deciding;

public class when_guarding_selection : given.an_activation
{
    ConsentRequest _decided;
    ConsentSelection _selection;

    void Establish()
    {
        _selection = new(binding.Tenant, binding.Scopes);
        _decided = request with { State = ConsentRequestState.Decided, Revision = 1, Selection = _selection };
    }

    [Fact] void should_refuse_approval_without_selection() => ConsentRequestTransitions.CanReplace(request, _decided with { Selection = null }).ShouldBeFalse();
    [Fact] void should_refuse_selection_on_denial() => ConsentRequestTransitions.CanReplace(request, _decided with { State = ConsentRequestState.Denied, PurgeAt = clock.Now + options.TerminalRetention }).ShouldBeFalse();
    [Fact] void should_refuse_late_selection_changes() => ConsentRequestTransitions.CanReplace(_decided, _decided with { State = ConsentRequestState.EvidenceRecorded, Revision = 2, Selection = _selection with { Tenant = "other" } }).ShouldBeFalse();
    [Fact] void should_preserve_selection_during_abandonment() => ConsentRequestTransitions.CanReplace(_decided, _decided with { State = ConsentRequestState.Abandoned, Revision = 2, PurgeAt = clock.Now + options.TerminalRetention }).ShouldBeTrue();
    [Fact] void should_refuse_late_selection_removal() => ConsentRequestTransitions.CanReplace(_decided, _decided with { State = ConsentRequestState.Abandoned, Revision = 2, Selection = null, PurgeAt = clock.Now + options.TerminalRetention }).ShouldBeFalse();
    [Fact] void should_refuse_created_at_changes() => ConsentRequestTransitions.CanReplace(request, _decided with { CreatedAt = clock.Now.AddSeconds(1) }).ShouldBeFalse();
}

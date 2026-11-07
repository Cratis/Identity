// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.ConsentFlow.Deciding;

public class when_evidence_exists_without_an_operational_activation : given.an_activation
{
    bool _usable;
    string? _code;

    async Task Because()
    {
        await evidence.Record(request, CancellationToken.None);
        activation = Restart();
        _usable = await authorizations.Usable(request.Id, store, fence);
        _code = await Claim();
    }

    [Fact] void should_not_enable_authorization_from_replayed_evidence() => _usable.ShouldBeFalse();
    [Fact] void should_not_issue_a_code_from_evidence_alone() => _code.ShouldBeNull();
}

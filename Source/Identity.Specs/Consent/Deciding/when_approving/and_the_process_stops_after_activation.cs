// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.ConsentFlow.Deciding.when_approving;

public class and_the_process_stops_after_activation : given.an_activation
{
    string? _first;
    string? _second;
    ConsentRequest _before;
    ConsentRequest _after;

    async Task Because()
    {
        await Approve();
        _before = await Resume();
        activation = Restart();
        _after = await Resume();
        _first = await Claim();
        activation = Restart();
        _second = await Claim();
    }

    [Fact] void should_not_change_activation_on_restart() => _after.ShouldEqual(_before);
    [Fact] void should_permit_one_code_issuance() => _first.ShouldNotBeNull();
    [Fact] void should_never_permit_a_second_code_issuance() => _second.ShouldBeNull();
    [Fact] void should_not_duplicate_evidence() => evidence.Appends.ShouldEqual(1);
    [Fact] void should_not_duplicate_the_grant() => authorizations.Count.ShouldEqual(1);
}

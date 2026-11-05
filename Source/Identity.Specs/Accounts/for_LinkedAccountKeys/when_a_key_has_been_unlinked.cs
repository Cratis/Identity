// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Accounts.for_LinkedAccountKeys;

public class when_a_key_has_been_unlinked : given.a_linked_keys_query
{
    protected override async Task Prepare()
    {
        await _linking.Confirm(Session(_account), _proposal, _proof);
        await Refresh();
        await _linking.Unlink(Session(_account), new(_proposal.Value));
        await Refresh();
    }

    Task Because() => Perform();

    [Fact] void should_hide_the_unlinked_key() => Keys.Any(key => key.LinkId.Value == _proposal.Value).ShouldBeFalse();
    [Fact] void should_keep_the_other_active_key() => Keys.Count().ShouldEqual(1);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Accounts.for_UpstreamKeyLinking;

public class when_trying_to_remove_the_last_key : given.a_link_proposal
{
    string _refusal;

    async Task Because()
    {
        var link = _states.InstanceForEventSourceId(_account.Value).Links.Values.Single().LinkId;
        _refusal = await Refusal(() => _linking.Unlink(Session(_account), link));
        await Refresh();
    }

    [Fact] void should_keep_one_authentication_key() => _states.InstanceForEventSourceId(_account.Value).Links.Count.ShouldEqual(1);
    [Fact] void should_refuse_the_operation() => _refusal.ShouldEqual("last_or_unknown_upstream_key");
}

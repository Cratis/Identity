// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Accounts.for_LinkedAccountKeys;

public class when_the_account_has_a_quarantined_proposal : given.a_linked_keys_query
{
    protected override bool Quarantine => true;

    Task Because() => Perform();

    [Fact] void should_show_quarantine_without_a_confirmation_time() => Keys.Count(key => key.State == "quarantined" && key.LinkedAt is null).ShouldEqual(1);
}

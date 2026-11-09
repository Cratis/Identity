// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Accounts.for_AccountMine;

public class when_the_owner_queries_with_a_spoofed_subject : given.an_owner_query
{
    Task Because() => Perform();

    [Fact] void should_authorize_the_owner() => _result.IsAuthorized.ShouldBeTrue();
    [Fact] void should_return_only_the_owners_profile() => ((Account)_result.Data).Id.ShouldEqual(_own.Id);
    [Fact] void should_ignore_the_supplied_target() => (_readSubject == _other.Id).ShouldBeFalse();
    [Fact] void should_filter_to_the_trusted_session_subject() => _readSubject.ShouldEqual(_own.Id);
}

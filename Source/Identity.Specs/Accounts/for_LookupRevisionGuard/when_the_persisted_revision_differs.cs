// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Accounts.for_LookupRevisionGuard;

public class when_the_persisted_revision_differs : given.a_revision_guard
{
    void Establish() => Stored([new(_key.Revision, _key.Check), new(_key.Revision - 1, _key.Check)], []);

    Task Because() => Start();

    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<AccountOperationRefused>();
    [Fact] void should_report_the_revision_mismatch() => _error.Message.ShouldEqual("lookup_key_revision_mismatch");
    [Fact] void should_not_write_over_existing_metadata() => Writes.ShouldEqual(0);
}

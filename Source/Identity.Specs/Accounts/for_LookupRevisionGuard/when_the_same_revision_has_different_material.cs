// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts.Lookup;

namespace Cratis.Identity.Accounts.for_LookupRevisionGuard;

public class when_the_same_revision_has_different_material : given.a_revision_guard
{
    void Establish() => Stored([new(_key.Revision, new UpstreamLookupKey(_key.Revision, new byte[32]).Check)], []);

    Task Because() => Start();

    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<AccountOperationRefused>();
    [Fact] void should_report_the_material_mismatch() => _error.Message.ShouldEqual("lookup_key_revision_mismatch");
    [Fact] void should_not_replace_the_retained_check() => Writes.ShouldEqual(0);
}

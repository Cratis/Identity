// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Operational.for_LookupRevisionGuard;

public class when_the_persisted_key_matches : given.a_revision_guard
{
    void Establish() => Stored([new(LookupRevision.SingletonId, _key.Check, _key.Revision)], []);

    Task Because() => Start();

    [Fact] void should_allow_startup() => _error.ShouldBeNull();
    [Fact] void should_only_attempt_one_metadata_write() => Writes.ShouldEqual(1);
    [Fact] void should_not_insert_another_record() => Inserts.ShouldEqual(0);
}

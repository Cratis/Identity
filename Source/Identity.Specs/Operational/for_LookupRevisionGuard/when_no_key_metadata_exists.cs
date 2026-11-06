// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Operational.for_LookupRevisionGuard;

public class when_no_key_metadata_exists : given.a_revision_guard
{
    void Establish() => Stored([], [new(LookupRevision.SingletonId, _key.Check, _key.Revision)]);

    Task Because() => Start();

    [Fact] void should_allow_the_first_startup() => _error.ShouldBeNull();
    [Fact] void should_create_the_singleton() => Inserts.ShouldEqual(1);
    [Fact] void should_verify_the_retained_check() => Reads.ShouldEqual(2);
}

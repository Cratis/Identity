// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Accounts.for_LookupRevisionGuard;

public class when_the_persisted_key_matches : given.a_revision_guard
{
    void Establish() => Stored([new(_key.Revision, _key.Check)], [new(_key.Revision, _key.Check)]);

    Task Because() => Start();

    [Fact] void should_allow_startup() => _error.ShouldBeNull();
    [Fact] void should_only_attempt_one_metadata_write() => Writes.ShouldEqual(1);
    [Fact] void should_request_an_upsert() => UsesUpsert.ShouldBeTrue();
    [Fact] void should_verify_the_stored_check_after_the_upsert() => Reads.ShouldEqual(2);
}

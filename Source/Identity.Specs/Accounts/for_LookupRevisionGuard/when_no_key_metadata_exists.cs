// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Accounts.for_LookupRevisionGuard;

public class when_no_key_metadata_exists : given.a_revision_guard
{
    void Establish() => Stored([], [new(_key.Revision, _key.Check)]);

    Task Because() => Start();

    [Fact] void should_allow_the_first_startup() => _error.ShouldBeNull();
    [Fact] void should_only_attempt_one_metadata_write() => Writes.ShouldEqual(1);
    [Fact] void should_request_metadata_creation() => UsesUpsert.ShouldBeTrue();
    [Fact] void should_verify_the_retained_check() => Reads.ShouldEqual(2);
}

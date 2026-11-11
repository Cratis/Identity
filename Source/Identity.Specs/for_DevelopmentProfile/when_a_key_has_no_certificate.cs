// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_DevelopmentProfile;

public class when_a_key_has_no_certificate : given.a_development_profile
{
    string _orphan;
    string[] _files;

    void Establish()
    {
        Apply();
        _orphan = Directory.EnumerateFiles(_storeDirectory, "signing.*.key").Single();
        File.Delete(Path.ChangeExtension(_orphan, ".crt"));
    }

    void Because()
    {
        Apply();
        _files = [.. Directory.EnumerateFiles(_storeDirectory).Select(Path.GetFileName)];
    }

    [Fact] void should_not_fail() => _exception.ShouldBeNull();
    [Fact] void should_remove_the_orphan_key() => File.Exists(_orphan).ShouldBeFalse();
    [Fact] void should_create_a_new_signing_pair_under_a_new_name() => _files.Count(_ => _.StartsWith("signing.dev-") && _.EndsWith(".crt") && _ != Path.GetFileName(Path.ChangeExtension(_orphan, ".crt"))).ShouldEqual(1);
    [Fact] void should_leave_a_key_for_every_certificate() => _files.Count(_ => _.EndsWith(".crt")).ShouldEqual(_files.Count(_ => _.EndsWith(".key")));
}

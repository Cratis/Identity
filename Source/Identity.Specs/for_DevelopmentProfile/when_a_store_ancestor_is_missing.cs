// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Development;
using Cratis.Identity.Hosting;

namespace Cratis.Identity.for_DevelopmentProfile;

public class when_a_store_ancestor_is_missing : given.a_development_profile
{
    bool _creationAttempted;
    string _missingParent;

    void Establish()
    {
        _missingParent = Path.Combine(_root, "cratis");
        Directory.Delete(_missingParent, recursive: true);
    }

    void Because() => _exception = Catch.Exception(() => new DevelopmentKeyStore(
        _root,
        _contentRoot,
        TimeProvider.System,
        beforeDirectoryCreation: _ => _creationAttempted = true).Provision(StoreName));

    [Fact] void should_require_the_user_to_create_the_parent_without_creating_any_store_directory()
    {
        _exception.ShouldBeOfExactType<DevelopmentProfileRefused>();
        _exception.Message.ShouldContain("create the parent directory");
        _creationAttempted.ShouldBeFalse();
        Directory.Exists(_missingParent).ShouldBeFalse();
        Directory.Exists(_storeDirectory).ShouldBeFalse();
    }
}

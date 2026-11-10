// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Hosting;

namespace Cratis.Identity.for_DevelopmentProfile;

public class when_the_directory_is_a_link_into_the_repository : given.a_development_profile
{
    string _repositoryDirectory;

    void Establish()
    {
        _repositoryDirectory = Path.Combine(_contentRoot, "keys");
        Directory.CreateDirectory(_repositoryDirectory);
        Directory.CreateDirectory(Path.GetDirectoryName(_storeDirectory));
        Directory.CreateSymbolicLink(_storeDirectory, _repositoryDirectory);
    }

    void Because() => Apply();

    [Fact] void should_refuse_it() => _exception.ShouldBeOfExactType<DevelopmentProfileRefused>();
    [Fact] void should_write_nothing_into_the_repository() => Directory.EnumerateFileSystemEntries(_repositoryDirectory).ShouldBeEmpty();
}

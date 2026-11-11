// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Hosting;

namespace Cratis.Identity.for_DevelopmentProfile;

public class when_the_vendor_folder_is_a_link : given.a_development_profile
{
    string _outside;

    void Establish()
    {
        _outside = Path.Combine(_contentRoot, "elsewhere");
        Directory.CreateDirectory(_outside);
        Directory.CreateSymbolicLink(Path.Combine(_root, "cratis"), _outside);
    }

    void Because() => Apply();

    [Fact] void should_refuse_it() => _exception.ShouldBeOfExactType<DevelopmentProfileRefused>();
    [Fact] void should_create_nothing_outside_the_tree() => Directory.EnumerateFileSystemEntries(_outside).ShouldBeEmpty();
}

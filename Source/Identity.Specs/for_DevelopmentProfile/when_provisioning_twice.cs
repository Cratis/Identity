// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_DevelopmentProfile;

public class when_provisioning_twice : given.a_development_profile
{
    string[] _first;
    string[] _second;

    void Because()
    {
        Apply();
        _first = Names();
        Apply();
        _second = Names();
    }

    [Fact] void should_not_fail() => _exception.ShouldBeNull();
    [Fact] void should_keep_the_same_keys() => _second.ShouldEqual(_first);

    string[] Names() => [.. Directory.EnumerateFiles(_storeDirectory).Select(_ => $"{Path.GetFileName(_)}:{File.GetLastWriteTimeUtc(_).Ticks}").Order(StringComparer.Ordinal)];
}

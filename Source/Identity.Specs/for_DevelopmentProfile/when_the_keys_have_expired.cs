// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.given;

namespace Cratis.Identity.for_DevelopmentProfile;

public class when_the_keys_have_expired : given.a_development_profile
{
    string[] _before;
    string[] _after;

    void Because()
    {
        Apply();
        _before = Files();
        Apply(new FixedTimeProvider(DateTimeOffset.UtcNow.AddDays(400)));
        _after = Files();
    }

    [Fact] void should_not_fail() => _exception.ShouldBeNull();
    [Fact] void should_keep_the_old_keys() => _before.All(_after.Contains).ShouldBeTrue();
    [Fact] void should_add_a_new_pair_of_each_purpose() => _after.Length.ShouldEqual(_before.Length + 4);

    string[] Files() => [.. Directory.EnumerateFiles(_storeDirectory, "*.dev-*").Select(Path.GetFileName)];
}

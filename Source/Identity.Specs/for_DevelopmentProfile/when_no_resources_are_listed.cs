// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Hosting;

namespace Cratis.Identity.for_DevelopmentProfile;

public class when_no_resources_are_listed : given.a_development_profile
{
    void Establish()
    {
        foreach (var key in _settings.Keys.Where(_ => _.StartsWith("Identity:Development:Resources")).ToList())
        {
            _settings.Remove(key);
        }
    }

    void Because() => Apply();

    [Fact] void should_refuse_it() => _exception.ShouldBeOfExactType<DevelopmentProfileRefused>();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Development;
using Cratis.Identity.Hosting;

namespace Cratis.Identity.for_DevelopmentProfile;

public class when_the_content_root_differs_only_in_case : given.a_development_profile
{
    bool _caseInsensitive;

    void Because()
    {
        _caseInsensitive = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS();
        _exception = Catch.Exception(() => new DevelopmentKeyStore(_root, Path.Combine(_root, "CRATIS").ToUpperInvariant(), TimeProvider.System).Provision(StoreName));
    }

    [Fact] void should_refuse_it_where_the_file_system_ignores_case() => (!_caseInsensitive || _exception is DevelopmentProfileRefused).ShouldBeTrue();
    [Fact] void should_compare_by_the_file_system_rules() => DevelopmentKeyStore.IsUnderOrEqual(Path.Combine(_root, "cratis", "x"), Path.Combine(_root, "CRATIS")).ShouldEqual(_caseInsensitive);
}

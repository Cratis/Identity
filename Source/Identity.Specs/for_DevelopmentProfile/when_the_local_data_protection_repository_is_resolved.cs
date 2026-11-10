// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Development;
using Cratis.Identity.Keys;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.for_DevelopmentProfile;

public class when_the_local_data_protection_repository_is_resolved : given.a_development_profile
{
    IXmlRepository _repository;

    void Because()
    {
        Apply();
        _exception.ShouldBeNull();
        _services.AddSecretStoreKeys(_startupOptions, _custody);
        using var provider = _services.BuildServiceProvider();
        _repository = provider.GetRequiredService<IOptions<KeyManagementOptions>>().Value.XmlRepository;
    }

    [Fact] void should_use_handle_custody_instead_of_a_path_based_file_system_repository() => _repository.ShouldBeOfExactType<DevelopmentXmlRepository>();
}

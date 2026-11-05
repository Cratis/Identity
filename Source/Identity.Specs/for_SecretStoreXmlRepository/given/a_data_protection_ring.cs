// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Cratis.Identity.given;
using Cratis.Identity.Keys;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Identity.for_SecretStoreXmlRepository.given;

public class a_data_protection_ring : Specification
{
    protected string _directory;
    protected StoreFiles _files;
    protected SecretStoreKeys _keys;
    protected SecretStoreXmlRepository _repository;
    protected Exception _exception;

    void Establish()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"identity-specs-{Guid.NewGuid():N}");
        _files = new StoreFiles()
            .Pair("signing", "a", KeyMaterial.CurrentRsa())
            .Pair("encryption", "a", KeyMaterial.CurrentRsa())
            .DataProtection("a", KeyMaterial.Now.AddDays(-1), KeyMaterial.Now.AddDays(90));
    }

    void Destroy()
    {
        _keys?.Dispose();
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, true);
        }
    }

    protected void Open()
    {
        _files.WriteTo(_directory);
        _keys = new SecretStoreKeys(new SecretStoreOptions { Path = _directory }, new FixedTimeProvider(KeyMaterial.Now), NullLogger<SecretStoreKeys>.Instance);
        _ = _keys.Current;
        _repository = new SecretStoreXmlRepository(_keys);
    }

    protected IDataProtectionProvider Provider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<TimeProvider>(new FixedTimeProvider(KeyMaterial.Now));
        services.AddDataProtection().SetApplicationName("Cratis.Identity").DisableAutomaticKeyGeneration();
        services.AddOptions<KeyManagementOptions>().Configure(options => options.XmlRepository = _repository);
        return services.BuildServiceProvider().GetRequiredService<IDataProtectionProvider>();
    }
}

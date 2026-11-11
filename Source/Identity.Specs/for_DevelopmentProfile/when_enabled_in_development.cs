// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;
using Cratis.Identity.Configuration;
using Cratis.Identity.Development;
using Cratis.Identity.Keys;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Identity.for_DevelopmentProfile;

public class when_enabled_in_development : given.a_development_profile
{
    SecretStoreKeys _keys;
    string[] _files;

    void Because()
    {
        Apply();
        _files = [.. Directory.EnumerateFiles(_storeDirectory).Select(Path.GetFileName)];
        _keys = new SecretStoreKeys(new SecretStoreOptions { Path = _storeDirectory }, TimeProvider.System, NullLogger<SecretStoreKeys>.Instance, providesDataProtection: false, developmentRoot: _custody);
        _ = _keys.Current;
    }

    void Destroy() => _keys?.Dispose();

    [Fact] void should_not_fail() => _exception.ShouldBeNull();
    [Fact] void should_keep_the_data_protection_ring_beside_the_keys() => _dataProtectionPath.EndsWith(Path.Combine("cratis", "identity-dev", StoreName, "data-protection"), StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_use_the_dedicated_directory_as_the_secret_store() => _startupOptions.SecretStore.Path.EndsWith(Path.Combine("cratis", "identity-dev", StoreName), StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_replace_the_resources() => _startupOptions.Resources.Select(_ => _.Audience).ShouldContainOnly(DevelopmentProfile.DirectMcpAudience, "studio-mcp");
    [Fact] void should_use_loopback_indicators() => _startupOptions.Resources.SelectMany(_ => _.Indicators).ShouldContainOnly("http://localhost:5200/mcp", "http://localhost:5300/mcp");
    [Fact] void should_register_the_profile() => _services.Any(_ => _.ServiceType == typeof(DevelopmentOptions)).ShouldBeTrue();
    [Fact] void should_write_the_provenance_marker() => File.ReadAllText(Path.Combine(_storeDirectory, DevelopmentKeyStore.MarkerFile)).ShouldEqual(DevelopmentKeyStore.Marker);
    [Fact] void should_write_a_signing_and_an_encryption_pair() => _files.Count(_ => _.StartsWith("signing.dev-") || _.StartsWith("encryption.dev-")).ShouldEqual(4);
    [Fact] void should_load_through_the_production_key_loader() => _keys.Current.Signer.Kid.ShouldNotBeNull();
    [Fact] void should_name_the_signer_as_a_development_key() => _keys.Current.Signer.Name.StartsWith("dev-").ShouldBeTrue();
    [Fact] void should_name_the_encrypter_as_a_development_key() => _keys.Current.Encrypter.Name.StartsWith("dev-").ShouldBeTrue();
    [Fact] void should_label_the_certificates_as_not_for_deployment() => X509Certificate2.CreateFromPem(File.ReadAllText(Directory.EnumerateFiles(_storeDirectory, "signing.*.crt").Single())).Subject.ShouldContain("NOT-FOR-DEPLOYMENT");
    [Fact] void should_create_owner_only_files() => OwnerOnly().ShouldBeTrue();

    bool OwnerOnly()
    {
        if (!OperatingSystem.IsWindows())
        {
            var modes = Directory.EnumerateFiles(_storeDirectory).Select(File.GetUnixFileMode).ToList();
            return modes.TrueForAll(_ => _ == (UnixFileMode.UserRead | UnixFileMode.UserWrite)) &&
                File.GetUnixFileMode(_storeDirectory) == (UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }

        return true;
    }
}

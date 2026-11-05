// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Cratis.Identity.given;
using Cratis.Identity.Keys;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Identity.for_SecretStoreKeys.given;

public class a_secret_store_directory : Specification
{
    protected string _directory;
    protected FixedTimeProvider _time;
    protected SecretStoreOptions _options;
    protected Exception _exception;
    protected SecretStoreKeys _keys;
    protected StoreFiles _files;

    void Establish()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"identity-specs-{Guid.NewGuid():N}");
        _time = new FixedTimeProvider(KeyMaterial.Now);
        _options = new SecretStoreOptions { Path = _directory };
        _files = new StoreFiles()
            .Pair("signing", "a", KeyMaterial.CurrentRsa())
            .Pair("encryption", "a", KeyMaterial.CurrentRsa())
            .DataProtection("a", KeyMaterial.Now.AddDays(-1), KeyMaterial.Now.AddDays(90));
        _files.WriteTo(_directory);
    }

    void Destroy()
    {
        _keys?.Dispose();
        Directory.Delete(_directory, true);
    }

    protected void Open() => _exception = Catch.Exception(() =>
    {
        _keys = new SecretStoreKeys(_options, _time, NullLogger<SecretStoreKeys>.Instance);
        _ = _keys.Current;
    });
}

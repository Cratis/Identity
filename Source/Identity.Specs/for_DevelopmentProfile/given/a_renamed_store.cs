// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Development;

namespace Cratis.Identity.for_DevelopmentProfile.given;

public class a_renamed_store : a_development_profile
{
    protected const string DecoyMarker = "not a development store";
    protected const string DecoyXml = "<decoy />";
    protected string _movedDirectory;
    protected string[] _decoyEntries;
    protected string _decoyXml;

    public static bool HasHandleRelativeCustody => OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() || OperatingSystem.IsLinux();

    protected void WithRenamedStore(Action use)
    {
        if (!HasHandleRelativeCustody)
        {
            return;
        }

        _movedDirectory = _storeDirectory + "-validated";
        var renamed = false;
        _exception = Catch.Exception(() =>
        {
            try
            {
                var store = new DevelopmentKeyStore(_root, _contentRoot, TimeProvider.System,
                    beforeStoreUse: directory =>
                    {
                        // Synchronous seam: validation and pinning have finished, but no marker or key has been touched.
                        Directory.Move(directory, _movedDirectory);
                        renamed = true;
                        Directory.CreateDirectory(directory);
                        File.WriteAllText(Path.Combine(directory, DevelopmentKeyStore.MarkerFile), DecoyMarker);
                        var decoyDataProtection = Path.Combine(directory, DevelopmentKeyStore.DataProtectionDirectory);
                        Directory.CreateDirectory(decoyDataProtection);
                        File.WriteAllText(Path.Combine(decoyDataProtection, "key-decoy.xml"), DecoyXml);
                    });
                store.Provision(StoreName);
                _custody = store.Custody;
                _custodies.Add(_custody);
                use();
                _decoyEntries = [.. Directory.EnumerateFileSystemEntries(_storeDirectory).Select(Path.GetFileName).Order(StringComparer.Ordinal)];
                _decoyXml = File.ReadAllText(Path.Combine(_storeDirectory, DevelopmentKeyStore.DataProtectionDirectory, "key-decoy.xml"));
            }
            finally
            {
                if (renamed)
                {
                    Directory.Delete(_storeDirectory, recursive: true);
                    Directory.Move(_movedDirectory, _storeDirectory);
                }
            }
        });
    }
}

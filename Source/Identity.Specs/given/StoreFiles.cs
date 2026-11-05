// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.given;

/// <summary>
/// The files of a secret store, built up by a spec.
/// </summary>
public class StoreFiles : Dictionary<string, string>
{
    public StoreFiles()
        : base(StringComparer.Ordinal)
    {
    }

    public StoreFiles Pair(string purpose, string name, PemPair pair)
    {
        this[$"{purpose}.{name}.crt"] = pair.Certificate;
        this[$"{purpose}.{name}.key"] = pair.Key;
        return this;
    }

    public StoreFiles DataProtection(string name, DateTimeOffset activation, DateTimeOffset expiration)
    {
        this[$"data-protection.{name}.xml"] = KeyMaterial.DataProtectionKey(activation, expiration);
        return this;
    }

    /// <summary>
    /// Writes the files to a directory, the way a mounted secret volume holds them.
    /// </summary>
    /// <param name="directory">The directory.</param>
    public void WriteTo(string directory)
    {
        Directory.CreateDirectory(directory);
        foreach (var file in this)
        {
            File.WriteAllText(Path.Combine(directory, file.Key), file.Value);
        }
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Keys;

/// <summary>
/// Reads the files of a mounted secret store directory.
/// </summary>
internal static class SecretStoreDirectory
{
    /// <summary>
    /// Reads the regular files directly in <paramref name="path"/>. Dot entries, such as the
    /// <c language="text">..data</c> directory Kubernetes maintains, and sub-directories are ignored. Symbolic links to
    /// files, which Kubernetes creates at the top level, are followed.
    /// </summary>
    /// <param name="path">The directory path.</param>
    /// <returns>The file contents by file name.</returns>
    public static IReadOnlyDictionary<string, string> Read(string path) =>
        Directory.EnumerateFileSystemEntries(path)
            .Where(entry => !Path.GetFileName(entry).StartsWith('.') && File.Exists(entry))
            .ToDictionary(entry => Path.GetFileName(entry), File.ReadAllText, StringComparer.Ordinal);
}

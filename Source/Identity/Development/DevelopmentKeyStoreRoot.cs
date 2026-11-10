// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Text;
using Cratis.Identity.Hosting;
using Microsoft.Win32.SafeHandles;

namespace Cratis.Identity.Development;

/// <summary>
/// Owns a validated, no-follow directory handle for the lifetime of the local profile. Pathnames are diagnostic only:
/// enumeration, child directories, files, atomic publication and deletion all use the pinned handle as their root.
/// </summary>
internal sealed class DevelopmentKeyStoreRoot : IDisposable
{
    readonly SafeFileHandle _handle;
    readonly KeyStoreFileIdentity _validatedIdentity;
    readonly Lock _lock = new();
    readonly Dictionary<string, DevelopmentKeyStoreRoot> _directories = new(StringComparer.Ordinal);
    bool _disposed;

    DevelopmentKeyStoreRoot(string directoryPath, SafeFileHandle handle)
    {
        DirectoryPath = directoryPath;
        _handle = handle;
        _validatedIdentity = KeyStoreHandles.Identity(handle);
        KeyStoreHandles.RequireOwnerOnly(handle, directory: true, "the key directory");
    }

    /// <summary>
    /// Gets the directory path for diagnostics only, never for custody operations.
    /// </summary>
    internal string DirectoryPath { get; }

    /// <inheritdoc/>
    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            foreach (var child in _directories.Values)
            {
                child.Dispose();
            }

            _handle.Dispose();
        }
    }

    /// <summary>
    /// Validates the existing ancestor chain top-down and creates only the last <paramref name="creatableSegments"/> segments, if absent.
    /// Each child is opened, or exclusively created owner-only, relative to its already validated parent handle, without following links or reparse points.
    /// Nothing above the creatable segments is ever created.
    /// </summary>
    /// <param name="directory">The canonical store location.</param>
    /// <param name="beforeDirectoryCreation">Optional synchronization seam before each directory creation, receiving that directory's path.</param>
    /// <param name="ancestorOwner">Optional ancestor ownership reader for specifications.</param>
    /// <param name="creatableSegments">How many trailing segments may be created when missing.</param>
    /// <returns>The custody of the validated leaf handle.</returns>
    /// <exception cref="DevelopmentProfileRefused">An ancestor is missing or the created leaf disappears before it can be pinned.</exception>
    internal static DevelopmentKeyStoreRoot Open(string directory, Action<string>? beforeDirectoryCreation = null, Func<string, int>? ancestorOwner = null, int creatableSegments = 1)
    {
        var path = Path.GetPathRoot(directory)!;
        var segments = directory[path.Length..].Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
        var current = KeyStoreHandles.OpenRoot(path);
        try
        {
            for (var index = 0; index < segments.Length; index++)
            {
                KeyStoreHandles.RequireSafeAncestor(current, path, ancestorOwner);
                var creatable = index >= segments.Length - creatableSegments;
                var next = KeyStoreHandles.Open(current, segments[index], directory: true);
                if (next is null)
                {
                    if (!creatable)
                    {
                        throw new DevelopmentProfileRefused($"the parent directory {Path.Combine(path, segments[index])} does not exist; create the parent directory before starting the local-development profile. Only the profile's own folders are created.");
                    }

                    beforeDirectoryCreation?.Invoke(Path.Combine(path, segments[index]));
                    KeyStoreHandles.CreateDirectory(current, segments[index]);
                    next = KeyStoreHandles.Open(current, segments[index], directory: true) ?? throw new DevelopmentProfileRefused("the key directory disappeared before it could be pinned.");
                }

                current.Dispose();
                current = next;
                path = Path.Combine(path, segments[index]);
            }

            var root = new DevelopmentKeyStoreRoot(directory, current);
            current = null!; // Ownership transfers to the process-lifetime custody object.
            return root;
        }
        finally
        {
            current?.Dispose();
        }
    }

    /// <summary>
    /// Enumerates entries after revalidating the pinned directory.
    /// </summary>
    /// <returns>The child names.</returns>
    internal IReadOnlyList<string> Entries()
    {
        lock (_lock)
        {
            VerifyIdentity();
            return KeyStoreHandles.Entries(_handle);
        }
    }

    /// <summary>
    /// Gets or creates validated custody of an owner-only child directory.
    /// </summary>
    /// <param name="name">The single child directory name.</param>
    /// <returns>The cached custody of the child directory.</returns>
    /// <exception cref="DevelopmentProfileRefused">The child disappears before it can be pinned.</exception>
    internal DevelopmentKeyStoreRoot GetDirectory(string name)
    {
        RequireName(name);
        lock (_lock)
        {
            VerifyIdentity();
            if (_directories.TryGetValue(name, out var existing))
            {
                existing.VerifyIdentity();
                return existing;
            }

            var handle = KeyStoreHandles.Open(_handle, name, directory: true);
            if (handle is null)
            {
                KeyStoreHandles.CreateDirectory(_handle, name);
                handle = KeyStoreHandles.Open(_handle, name, directory: true) ?? throw new DevelopmentProfileRefused($"the key directory {name} disappeared before it could be pinned.");
            }

            try
            {
                var child = new DevelopmentKeyStoreRoot(Path.Combine(DirectoryPath, name), handle);
                _directories.Add(name, child);
                return child;
            }
            catch
            {
                handle.Dispose();
                throw;
            }
        }
    }

    /// <summary>
    /// Opens a child file for reading after validating its actual handle.
    /// </summary>
    /// <param name="name">The single child file name.</param>
    /// <returns>A stream owning the validated file handle.</returns>
    /// <exception cref="DevelopmentProfileRefused">The file disappears before it can be opened.</exception>
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The returned FileStream owns the SafeFileHandle on success; the catch disposes it if validation or stream construction fails.")]
    internal FileStream OpenRead(string name)
    {
        RequireName(name);
        lock (_lock)
        {
            VerifyIdentity();
            var handle = KeyStoreHandles.Open(_handle, name, directory: false) ?? throw new DevelopmentProfileRefused($"the key file {name} disappeared before it could be read.");
            try
            {
                KeyStoreHandles.RequireOwnerOnly(handle, directory: false, $"the key file {name}");
                return new FileStream(handle, FileAccess.Read);
            }
            catch
            {
                handle.Dispose();
                throw;
            }
        }
    }

    /// <summary>
    /// Reads a validated child file as UTF-8 text.
    /// </summary>
    /// <param name="name">The single child file name.</param>
    /// <returns>The file content.</returns>
    internal string Read(string name)
    {
        using var stream = OpenRead(name);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Reads the development key files after checking provenance and rejecting unexpected entries.
    /// </summary>
    /// <returns>The key file content indexed by name.</returns>
    /// <exception cref="DevelopmentProfileRefused">The directory contains an unexpected entry.</exception>
    internal IReadOnlyDictionary<string, string> ReadKeyFiles()
    {
        lock (_lock)
        {
            RequireMarker();
            var files = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var name in Entries().Where(_ => _ != DevelopmentKeyStore.MarkerFile && _ != DevelopmentKeyStore.DataProtectionDirectory))
            {
                if (!DevelopmentKeyStore.IsDevelopmentFile(name))
                {
                    throw new DevelopmentProfileRefused($"the key directory holds an unexpected entry ({name}); only development key files are allowed.");
                }

                files.Add(name, Read(name));
            }

            return files;
        }
    }

    /// <summary>
    /// Requires the development provenance marker to contain its expected value.
    /// </summary>
    /// <exception cref="DevelopmentProfileRefused">The marker content is invalid.</exception>
    internal void RequireMarker()
    {
        if (!string.Equals(Read(DevelopmentKeyStore.MarkerFile).Trim(), DevelopmentKeyStore.Marker, StringComparison.Ordinal))
        {
            throw new DevelopmentProfileRefused("the development provenance marker is not valid.");
        }
    }

    /// <summary>
    /// Writes and flushes an owner-only temporary file, then publishes it without replacing existing content.
    /// </summary>
    /// <param name="name">The single destination file name.</param>
    /// <param name="content">The UTF-8 text to persist.</param>
    internal void WriteAtomically(string name, string content)
    {
        RequireName(name);
        lock (_lock)
        {
            VerifyIdentity();
            var temporary = $"{DevelopmentKeyStore.TemporaryPrefix}{Guid.NewGuid():N}";
            using var handle = KeyStoreHandles.Open(_handle, temporary, directory: false, create: true)!;
            try
            {
                KeyStoreHandles.RequireOwnerOnly(handle, directory: false, $"the key file {name}");
                using var stream = new FileStream(handle, FileAccess.Write);
                stream.Write(Encoding.UTF8.GetBytes(content));
                stream.Flush(flushToDisk: true);
                KeyStoreHandles.Publish(_handle, temporary, name, handle);
            }
            finally
            {
                KeyStoreHandles.Delete(_handle, temporary);
            }
        }
    }

    /// <summary>
    /// Deletes a child file through the revalidated pinned directory.
    /// </summary>
    /// <param name="name">The single child file name.</param>
    internal void Delete(string name)
    {
        RequireName(name);
        lock (_lock)
        {
            VerifyIdentity();
            KeyStoreHandles.Delete(_handle, name);
        }
    }

    /// <summary>
    /// Revalidates the identity and owner-only permissions of the pinned directory.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Custody has been disposed.</exception>
    /// <exception cref="DevelopmentProfileRefused">The pinned directory identity has changed.</exception>
    internal void VerifyIdentity()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_validatedIdentity.SameObject(KeyStoreHandles.Identity(_handle)))
        {
            throw new DevelopmentProfileRefused("the pinned key directory identity changed after validation; no key material will be used.");
        }

        KeyStoreHandles.RequireOwnerOnly(_handle, directory: true, "the key directory");
    }

    static void RequireName(string name)
    {
        if (!IsSingleName(name))
        {
            throw new DevelopmentProfileRefused("a development key entry must be a single file name, not a path.");
        }
    }

    static bool IsSingleName(string name) => !string.IsNullOrWhiteSpace(name) && name is not "." and not ".." && name.IndexOfAny(['/', '\\', ':', '\0']) < 0;
}

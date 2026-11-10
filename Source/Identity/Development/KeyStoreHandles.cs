// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Hosting;
using Microsoft.Win32.SafeHandles;

namespace Cratis.Identity.Development;

/// <summary>
/// Dispatches development custody operations to native no-follow, directory-relative APIs.
/// </summary>
internal static class KeyStoreHandles
{
    /// <summary>
    /// Opens the filesystem root without following links.
    /// </summary>
    /// <param name="path">The filesystem root path.</param>
    /// <returns>An owned root handle.</returns>
    internal static SafeFileHandle OpenRoot(string path) => OperatingSystem.IsWindows() ? WindowsKeyStoreHandles.OpenRoot(path) : UnixKeyStoreHandles.OpenRoot(path);

    /// <summary>
    /// Opens or exclusively creates a child relative to its pinned parent without following links.
    /// </summary>
    /// <param name="parent">The pinned parent handle.</param>
    /// <param name="name">The child name.</param>
    /// <param name="directory">Whether the child is a directory.</param>
    /// <param name="create">Whether to create a new child with owner-only permissions.</param>
    /// <returns>An owned child handle, or null if an existing child was requested but not found.</returns>
    internal static SafeFileHandle? Open(SafeFileHandle parent, string name, bool directory, bool create = false) =>
        OperatingSystem.IsWindows() ? WindowsKeyStoreHandles.Open(parent, name, directory, create) : UnixKeyStoreHandles.Open(parent, name, directory, create);

    /// <summary>
    /// Reads identity and security metadata from an open handle.
    /// </summary>
    /// <param name="handle">The handle to inspect.</param>
    /// <returns>The object's identity and metadata.</returns>
    internal static KeyStoreFileIdentity Identity(SafeFileHandle handle) => OperatingSystem.IsWindows() ? WindowsKeyStoreHandles.Identity(handle) : UnixKeyStoreHandles.Identity(handle);

    /// <summary>
    /// Enumerates child names through the pinned directory handle.
    /// </summary>
    /// <param name="handle">The pinned directory handle.</param>
    /// <returns>The child names, excluding dot entries.</returns>
    internal static IReadOnlyList<string> Entries(SafeFileHandle handle) => OperatingSystem.IsWindows() ? WindowsKeyStoreHandles.Entries(handle) : UnixKeyStoreHandles.Entries(handle);

    /// <summary>
    /// Creates an owner-only directory relative to its pinned parent.
    /// </summary>
    /// <param name="parent">The pinned parent handle.</param>
    /// <param name="name">The child directory name.</param>
    internal static void CreateDirectory(SafeFileHandle parent, string name)
    {
        if (OperatingSystem.IsWindows())
        {
            WindowsKeyStoreHandles.CreateDirectory(parent, name);
        }
        else
        {
            UnixKeyStoreHandles.CreateDirectory(parent, name);
        }
    }

    /// <summary>
    /// Atomically publishes a completed temporary file without replacing an existing destination.
    /// </summary>
    /// <param name="parent">The pinned parent handle.</param>
    /// <param name="temporary">The temporary file name.</param>
    /// <param name="name">The destination name.</param>
    /// <param name="file">The open temporary file handle.</param>
    internal static void Publish(SafeFileHandle parent, string temporary, string name, SafeFileHandle file)
    {
        if (OperatingSystem.IsWindows())
        {
            WindowsKeyStoreHandles.Publish(parent, name, file);
        }
        else
        {
            UnixKeyStoreHandles.Publish(parent, temporary, name);
        }
    }

    /// <summary>
    /// Removes a child file relative to its pinned parent, allowing an already absent file.
    /// </summary>
    /// <param name="parent">The pinned parent handle.</param>
    /// <param name="name">The child file name.</param>
    internal static void Delete(SafeFileHandle parent, string name)
    {
        if (OperatingSystem.IsWindows())
        {
            WindowsKeyStoreHandles.Delete(parent, name);
        }
        else
        {
            UnixKeyStoreHandles.Delete(parent, name);
        }
    }

    /// <summary>
    /// Requires the expected object type and owner-only access on an open handle.
    /// </summary>
    /// <param name="handle">The handle to validate.</param>
    /// <param name="directory">Whether a directory is required.</param>
    /// <param name="what">The diagnostic description of the object.</param>
    internal static void RequireOwnerOnly(SafeFileHandle handle, bool directory, string what)
    {
        var identity = Identity(handle);
        RequireType(identity, directory, what);
        if (OperatingSystem.IsWindows())
        {
            WindowsKeyStoreHandles.RequireOwnerOnly(handle, what);
            return;
        }

        UnixKeyStoreHandles.RequireOwnerOnly(handle, identity, what);
    }

    /// <summary>
    /// Requires an ancestor directory whose ownership and permissions prevent replacement by other users.
    /// </summary>
    /// <param name="handle">The pinned ancestor handle.</param>
    /// <param name="path">The diagnostic ancestor path.</param>
    /// <param name="ancestorOwner">An optional ownership reader for specifications.</param>
    /// <exception cref="DevelopmentProfileRefused">The ancestor is not a directory.</exception>
    internal static void RequireSafeAncestor(SafeFileHandle handle, string path, Func<string, int>? ancestorOwner)
    {
        var identity = Identity(handle);
        if (!identity.IsDirectory)
        {
            throw new DevelopmentProfileRefused($"the directory {path} above the key directory is not a directory.");
        }

        if (OperatingSystem.IsWindows())
        {
            WindowsKeyStoreHandles.RequireSafeAncestor(handle, path);
            return;
        }

        UnixKeyStoreHandles.RequireSafeAncestor(handle, identity, path, ancestorOwner);
    }

    static void RequireType(KeyStoreFileIdentity identity, bool directory, string what)
    {
        if (directory ? !identity.IsDirectory : !identity.IsFile)
        {
            throw new DevelopmentProfileRefused($"{what} is not a regular {(directory ? "directory" : "file")}; the profile never follows links or uses special files.");
        }
    }
}

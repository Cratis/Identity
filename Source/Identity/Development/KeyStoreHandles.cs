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
    internal static SafeFileHandle OpenRoot(string path) => OperatingSystem.IsWindows() ? WindowsKeyStoreHandles.OpenRoot(path) : UnixKeyStoreHandles.OpenRoot(path);

    internal static SafeFileHandle? Open(SafeFileHandle parent, string name, bool directory, bool create = false) =>
        OperatingSystem.IsWindows() ? WindowsKeyStoreHandles.Open(parent, name, directory, create) : UnixKeyStoreHandles.Open(parent, name, directory, create);

    internal static KeyStoreFileIdentity Identity(SafeFileHandle handle) => OperatingSystem.IsWindows() ? WindowsKeyStoreHandles.Identity(handle) : UnixKeyStoreHandles.Identity(handle);

    internal static IReadOnlyList<string> Entries(SafeFileHandle handle) => OperatingSystem.IsWindows() ? WindowsKeyStoreHandles.Entries(handle) : UnixKeyStoreHandles.Entries(handle);

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

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Cratis.Identity.Hosting;
using Microsoft.Win32.SafeHandles;

namespace Cratis.Identity.Development;

/// <summary>
/// Refuses macOS extended ACL grants, including inherited and inherit-only grants that POSIX modes do not restrict.
/// Deny entries are safe: they cannot expose keys or permit replacement of an ancestor.
/// </summary>
[SupportedOSPlatform("macos")]
internal static partial class MacExtendedAccess
{
    const int ExtendedAcl = 0x100;
    const int NoAcl = 2;
    const int ReadOnlyDirectoryNoFollow = 0x100000 | 0x100;
    const int ExtendedDeny = 2;

    /// <summary>
    /// Darwin returns EINVAL at the end of a valid ACL, including an empty ACL.
    /// </summary>
    const int EndOfEntries = 22;

    /// <summary>
    /// Checks the extended ACL of a directory before it can supply inherited access to newly created keys.
    /// </summary>
    /// <param name="path">The directory.</param>
    /// <exception cref="DevelopmentProfileRefused">The directory cannot be safely opened or its ACL grants access.</exception>
    public static void RequireNoGrants(string path)
    {
        var descriptor = OpenDirectory(path, ReadOnlyDirectoryNoFollow);
        if (descriptor < 0)
        {
            throw Unreadable();
        }

        using var handle = new SafeFileHandle(descriptor, ownsHandle: true);
        RequireNoGrants(handle);
    }

    /// <summary>
    /// Checks the extended ACL on the same handle used to read or write a key file.
    /// </summary>
    /// <param name="handle">The open file handle.</param>
    public static void RequireNoGrants(SafeFileHandle handle)
    {
        var acl = GetHandleAcl(handle, ExtendedAcl);

        // On Darwin ENOENT means no extended ACL. Using an open handle disambiguates this from a missing path.
        if (acl == IntPtr.Zero && Marshal.GetLastPInvokeError() == NoAcl)
        {
            return;
        }

        RequireNoGrants(acl);
    }

    static void RequireNoGrants(IntPtr acl)
    {
        if (acl == IntPtr.Zero)
        {
            throw Unreadable();
        }

        try
        {
            if (ValidateAcl(acl) != 0)
            {
                throw Unreadable();
            }

            RequireDenyOnlyEntries(acl);
        }
        finally
        {
            // This is the allocation returned by acl_get_fd_np; releasing it cannot change the decision.
            _ = FreeAcl(acl);
        }
    }

    static void RequireDenyOnlyEntries(IntPtr acl)
    {
        var entryId = 0; // ACL_FIRST_ENTRY
        while (GetEntry(acl, entryId, out var entry) == 0)
        {
            if (GetTag(entry, out var tag) != 0)
            {
                throw Unreadable();
            }

            if (tag != ExtendedDeny)
            {
                throw new DevelopmentProfileRefused("the key store or an ancestor has an unexpected extended ACL grant; POSIX owner-only permissions do not restrict it.");
            }

            entryId = -1; // ACL_NEXT_ENTRY
        }

        if (Marshal.GetLastPInvokeError() != EndOfEntries)
        {
            throw Unreadable();
        }
    }

    static DevelopmentProfileRefused Unreadable() => new("the macOS extended ACL cannot be read, so the key store cannot be shown safe.");

    [LibraryImport("/usr/lib/libSystem.B.dylib", EntryPoint = "open", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static partial int OpenDirectory(string path, int flags);

    [LibraryImport("/usr/lib/libSystem.B.dylib", EntryPoint = "acl_get_fd_np", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static partial IntPtr GetHandleAcl(SafeFileHandle handle, int type);

    [LibraryImport("/usr/lib/libSystem.B.dylib", EntryPoint = "acl_valid", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static partial int ValidateAcl(IntPtr acl);

    [LibraryImport("/usr/lib/libSystem.B.dylib", EntryPoint = "acl_get_entry", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static partial int GetEntry(IntPtr acl, int entryId, out IntPtr entry);

    [LibraryImport("/usr/lib/libSystem.B.dylib", EntryPoint = "acl_get_tag_type", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static partial int GetTag(IntPtr entry, out int tag);

    [LibraryImport("/usr/lib/libSystem.B.dylib", EntryPoint = "acl_free")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static partial int FreeAcl(IntPtr acl);
}

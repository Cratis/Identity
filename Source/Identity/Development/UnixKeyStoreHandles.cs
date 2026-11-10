// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Cratis.Identity.Hosting;
using Microsoft.Win32.SafeHandles;

namespace Cratis.Identity.Development;

/// <summary>
/// The POSIX directory-relative operations used by development key custody. No operation follows a symbolic link.
/// </summary>
[UnsupportedOSPlatform("windows")]
internal static partial class UnixKeyStoreHandles
{
    const int Exists = 17;
    const int Missing = 2;

    /// <summary>
    /// The owner-only directory mode (0700).
    /// </summary>
    const uint DirectoryMode = 448;

    /// <summary>
    /// The owner-only file mode (0600).
    /// </summary>
    const uint FileMode = 384;

    /// <summary>
    /// Gets the effective user identifier used for ownership validation.
    /// </summary>
    internal static uint CurrentUser => GetEffectiveUser();

    static bool IsLinuxArm => OperatingSystem.IsLinux() && RuntimeInformation.ProcessArchitecture is Architecture.Arm or Architecture.Arm64;

    /// <summary>
    /// Gets the no-follow file flags combined with O_DIRECTORY.
    /// </summary>
    static int DirectoryFlags => FileFlags | (OperatingSystem.IsMacOS() ? 0x100000 : IsLinuxArm ? 0x4000 : 0x10000);

    /// <summary>
    /// Gets O_NOFOLLOW, O_CLOEXEC and O_NONBLOCK for the platform ABI.
    /// </summary>
    static int FileFlags => OperatingSystem.IsMacOS() ? 0x1000104 : IsLinuxArm ? 0x88800 : 0xa0800;

    /// <inheritdoc cref="KeyStoreHandles.OpenRoot"/>
    internal static SafeFileHandle OpenRoot(string path) => Own(OpenAt(-1, path, DirectoryFlags, 0), "open the filesystem root");

    /// <inheritdoc cref="KeyStoreHandles.Open"/>
    internal static SafeFileHandle? Open(SafeFileHandle parent, string name, bool directory, bool create = false)
    {
        var flags = directory ? DirectoryFlags : FileFlags;
        if (create)
        {
            flags |= OperatingSystem.IsMacOS() ? 0xa01 : 0xc1; // O_WRONLY | O_CREAT | O_EXCL
        }

        var fd = OpenAt(parent.DangerousGetHandle().ToInt32(), name, flags, FileMode);
        if (fd < 0 && !create && Marshal.GetLastPInvokeError() == Missing)
        {
            return null;
        }

        return Own(fd, $"open {name} without following links");
    }

    /// <inheritdoc cref="KeyStoreHandles.CreateDirectory"/>
    internal static void CreateDirectory(SafeFileHandle parent, string name)
    {
        if (MakeDirectoryAt(parent, name, DirectoryMode) != 0 && Marshal.GetLastPInvokeError() != Exists)
        {
            throw Refused($"create directory {name}");
        }
    }

    /// <inheritdoc cref="KeyStoreHandles.Identity"/>
    /// <exception cref="DevelopmentProfileRefused">The operating system does not support handle-relative custody.</exception>
    internal static KeyStoreFileIdentity Identity(SafeFileHandle handle)
    {
        if (OperatingSystem.IsMacOS())
        {
            return DarwinIdentity(handle);
        }

        if (OperatingSystem.IsLinux())
        {
            return LinuxIdentity(handle);
        }

        throw new DevelopmentProfileRefused("handle-relative development key custody is supported only on Windows, macOS and Linux.");
    }

    /// <summary>
    /// Enumerates child names using a fresh directory stream with its own offset.
    /// </summary>
    /// <param name="parent">The pinned directory handle.</param>
    /// <returns>The child names, excluding dot entries.</returns>
    /// <exception cref="DevelopmentProfileRefused">The directory stream cannot be opened or enumerated.</exception>
    internal static IReadOnlyList<string> Entries(SafeFileHandle parent)
    {
        // A fresh open of '.' gives enumeration its own offset, unlike dup of the pinned descriptor.
        using var enumeration = Open(parent, ".", directory: true)!;
        var directory = OpenStream(enumeration.DangerousGetHandle().ToInt32());
        if (directory == IntPtr.Zero)
        {
            throw Refused("enumerate the pinned key directory");
        }

        enumeration.SetHandleAsInvalid(); // fdopendir owns this descriptor now.
        try
        {
            var names = new List<string>();
            while (true)
            {
                var entry = NextEntry(directory);
                if (entry == IntPtr.Zero)
                {
                    if (Marshal.GetLastPInvokeError() != 0)
                    {
                        throw Refused("enumerate the pinned key directory");
                    }

                    return names;
                }

                var name = Marshal.PtrToStringUTF8(entry + (OperatingSystem.IsMacOS() ? 21 : 19))!;
                if (name is not "." and not "..")
                {
                    names.Add(name);
                }
            }
        }
        finally
        {
            _ = CloseDirectoryStream(directory);
        }
    }

    /// <summary>
    /// Atomically links a completed temporary file to its destination without replacing an existing file.
    /// </summary>
    /// <param name="parent">The pinned parent handle.</param>
    /// <param name="temporary">The temporary file name.</param>
    /// <param name="name">The destination name.</param>
    /// <exception cref="DevelopmentProfileRefused">The file cannot be published without replacing existing content.</exception>
    internal static void Publish(SafeFileHandle parent, string temporary, string name)
    {
        // linkat publishes a complete file atomically and, unlike renameat, never replaces an existing destination.
        if (LinkAt(parent, temporary, parent, name, 0) != 0)
        {
            throw Refused($"publish {name}");
        }
    }

    /// <inheritdoc cref="KeyStoreHandles.Delete"/>
    internal static void Delete(SafeFileHandle parent, string name)
    {
        if (UnlinkAt(parent, name, 0) != 0 && Marshal.GetLastPInvokeError() != Missing)
        {
            throw Refused($"remove {name}");
        }
    }

    /// <summary>
    /// Requires current-user ownership, owner-only mode bits and no extended access grants.
    /// </summary>
    /// <param name="handle">The handle to validate for extended access.</param>
    /// <param name="identity">The metadata read from the handle.</param>
    /// <param name="what">The diagnostic description of the object.</param>
    /// <exception cref="DevelopmentProfileRefused">The object permits other users access or belongs to another user.</exception>
    internal static void RequireOwnerOnly(SafeFileHandle handle, KeyStoreFileIdentity identity, string what)
    {
        var ownerMode = identity.IsDirectory ? (int)DirectoryMode : (int)FileMode;
        if ((identity.Mode & 0xfff & ~ownerMode) != 0)
        {
            throw new DevelopmentProfileRefused(identity.IsDirectory
                ? "the key directory is accessible to other users; remove it so it can be recreated with owner-only permissions."
                : $"{what} is accessible to other users; remove the key directory so it can be recreated.");
        }

        if (identity.Owner != CurrentUser)
        {
            throw new DevelopmentProfileRefused($"{what} is not owned by the current user; remove it so it can be recreated.");
        }

        RequireExtendedAccess(handle);
    }

    /// <summary>
    /// Requires ancestor ownership and permissions that prevent replacement by other users.
    /// </summary>
    /// <param name="handle">The pinned ancestor handle.</param>
    /// <param name="identity">The metadata read from the handle.</param>
    /// <param name="path">The diagnostic ancestor path.</param>
    /// <param name="ancestorOwner">An optional ownership reader for specifications.</param>
    /// <exception cref="DevelopmentProfileRefused">The ancestor is owned by another user or is unsafely writable by others.</exception>
    internal static void RequireSafeAncestor(SafeFileHandle handle, KeyStoreFileIdentity identity, string path, Func<string, int>? ancestorOwner)
    {
        var owner = ancestorOwner is null ? identity.Owner : checked((uint)ancestorOwner(path));
        if (owner != CurrentUser && owner != 0)
        {
            throw new DevelopmentProfileRefused($"the directory {path} above the key directory is owned by another user, who could replace the key store.");
        }

        var writableByOthers = (identity.Mode & 0x12) != 0; // S_IWGRP | S_IWOTH
        var acceptedStickyDirectory = owner == 0 && (identity.Mode & 0x200) != 0;
        if (writableByOthers && !acceptedStickyDirectory)
        {
            throw new DevelopmentProfileRefused($"the directory {path} above the key directory is writable by other users, who could replace the key store.");
        }

        RequireExtendedAccess(handle);
    }

    static void RequireExtendedAccess(SafeFileHandle handle)
    {
        if (OperatingSystem.IsMacOS())
        {
            MacExtendedAccess.RequireNoGrants(handle);
        }
    }

    static int OpenAt(int directory, string name, int flags, uint mode)
    {
        // Apple's arm64 ABI passes variadic arguments on the stack, not in x3. Fill the unused register
        // argument slots so the promoted mode argument occupies the first stack slot consumed by openat.
        return OperatingSystem.IsMacOS() && RuntimeInformation.ProcessArchitecture == Architecture.Arm64
            ? OpenAtDarwinArm64(directory, name, flags, 0, 0, 0, 0, 0, mode)
            : OpenAtNative(directory, name, flags, mode);
    }

    static IntPtr OpenStream(int descriptor) => OperatingSystem.IsMacOS() && RuntimeInformation.ProcessArchitecture == Architecture.X64
        ? OpenDirectoryStreamInode64(descriptor)
        : OpenDirectoryStream(descriptor);

    static IntPtr NextEntry(IntPtr directory)
    {
        if (!OperatingSystem.IsMacOS())
        {
            return ReadDirectory64(directory);
        }

        return RuntimeInformation.ProcessArchitecture == Architecture.X64 ? ReadDirectoryInode64(directory) : ReadDirectory(directory);
    }

    static SafeFileHandle Own(int fd, string operation) => fd < 0 ? throw Refused(operation) : new SafeFileHandle(fd, ownsHandle: true);

    static DevelopmentProfileRefused Refused(string operation) => new($"cannot {operation} in the development key store (system error {Marshal.GetLastPInvokeError()}); no key material was adopted.");

    static unsafe KeyStoreFileIdentity DarwinIdentity(SafeFileHandle handle)
    {
        // Darwin's 64-bit struct stat layout is shared by its supported x64 and arm64 ABIs.
        var status = stackalloc byte[256];
        var result = RuntimeInformation.ProcessArchitecture == Architecture.X64 ? DarwinStatInode64(handle, status) : DarwinStat(handle, status);
        if (result != 0)
        {
            throw Refused("identify the pinned key handle");
        }

        return new KeyStoreFileIdentity(*(uint*)status, *(ulong*)(status + 8), *(uint*)(status + 16), *(ushort*)(status + 4));
    }

    static unsafe KeyStoreFileIdentity LinuxIdentity(SafeFileHandle handle)
    {
        // statx has a fixed, architecture-independent layout. AT_EMPTY_PATH queries this open descriptor, not a name.
        var status = stackalloc byte[256];
        const uint required = 0x10b; // STATX_TYPE | STATX_MODE | STATX_UID | STATX_INO
        if (LinuxStat(handle, string.Empty, 0x1000, required, status) != 0 || (*(uint*)status & required) != required)
        {
            throw Refused("identify the pinned key handle");
        }

        var device = ((ulong)*(uint*)(status + 136) << 32) | *(uint*)(status + 140);
        return new KeyStoreFileIdentity(device, *(ulong*)(status + 32), *(uint*)(status + 20), *(ushort*)(status + 28));
    }

    [LibraryImport("libc", EntryPoint = "geteuid")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static partial uint GetEffectiveUser();

    [LibraryImport("libc", EntryPoint = "openat", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static partial int OpenAtNative(int directory, string name, int flags, uint mode);

    [LibraryImport("/usr/lib/libSystem.B.dylib", EntryPoint = "openat", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static partial int OpenAtDarwinArm64(int directory, string name, int flags, nint register3, nint register4, nint register5, nint register6, nint register7, uint mode);

    [LibraryImport("libc", EntryPoint = "mkdirat", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static partial int MakeDirectoryAt(SafeFileHandle directory, string name, uint mode);

    [LibraryImport("libc", EntryPoint = "linkat", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static partial int LinkAt(SafeFileHandle source, string sourceName, SafeFileHandle destination, string destinationName, int flags);

    [LibraryImport("libc", EntryPoint = "unlinkat", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static partial int UnlinkAt(SafeFileHandle directory, string name, int flags);

    [LibraryImport("libc", EntryPoint = "fdopendir", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static partial IntPtr OpenDirectoryStream(int directory);

    [LibraryImport("libc", EntryPoint = "readdir", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static partial IntPtr ReadDirectory(IntPtr directory);

    [LibraryImport("/usr/lib/libSystem.B.dylib", EntryPoint = "fdopendir$INODE64", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static partial IntPtr OpenDirectoryStreamInode64(int directory);

    [LibraryImport("/usr/lib/libSystem.B.dylib", EntryPoint = "readdir$INODE64", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static partial IntPtr ReadDirectoryInode64(IntPtr directory);

    [LibraryImport("libc", EntryPoint = "readdir64", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static partial IntPtr ReadDirectory64(IntPtr directory);

    [LibraryImport("libc", EntryPoint = "closedir")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static partial int CloseDirectoryStream(IntPtr directory);

    [LibraryImport("/usr/lib/libSystem.B.dylib", EntryPoint = "fstat", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static unsafe partial int DarwinStat(SafeFileHandle file, byte* status);

    [LibraryImport("/usr/lib/libSystem.B.dylib", EntryPoint = "fstat$INODE64", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static unsafe partial int DarwinStatInode64(SafeFileHandle file, byte* status);

    [LibraryImport("libc", EntryPoint = "statx", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static unsafe partial int LinuxStat(SafeFileHandle file, string name, int flags, uint mask, byte* status);
}

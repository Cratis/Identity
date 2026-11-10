// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using Cratis.Identity.Hosting;
using Microsoft.Win32.SafeHandles;

namespace Cratis.Identity.Development;

/// <summary>
/// Windows handle-relative custody. NtCreateFile's RootDirectory binds every child open to the pinned directory;
/// FILE_OPEN_REPARSE_POINT and OBJ_DONT_REPARSE prevent following a substituted reparse point.
/// </summary>
[SupportedOSPlatform("windows")]
internal static partial class WindowsKeyStoreHandles
{
    const uint MaximumAllowed = 0x02000000;
    const uint ShareAll = 7;
    const int NameNotFound = unchecked((int)0xc0000034);
    const int PathNotFound = unchecked((int)0xc000003a);
    const int NoSuchFile = unchecked((int)0xc000000f);
    const int NoMoreFiles = unchecked((int)0x80000006);

    internal static SafeFileHandle OpenRoot(string path)
    {
        var handle = CreateFile(path, MaximumAllowed, ShareAll, IntPtr.Zero, 3, 0x02200000, IntPtr.Zero);
        if (handle.IsInvalid)
        {
            handle.Dispose();
            throw Refused("open the filesystem root");
        }

        return handle;
    }

    internal static unsafe SafeFileHandle? Open(SafeFileHandle parent, string name, bool directory, bool create = false)
    {
        var descriptor = create ? OwnerOnlyDescriptor(directory) : [];
        fixed (char* characters = name)
        fixed (byte* security = descriptor)
        {
            var unicode = new UnicodeName { Length = checked((ushort)(name.Length * 2)), MaximumLength = checked((ushort)(name.Length * 2)), Buffer = (IntPtr)characters };
            var attributes = new ObjectAttributes
            {
                Length = (uint)Marshal.SizeOf<ObjectAttributes>(),
                RootDirectory = parent.DangerousGetHandle(),
                ObjectName = (IntPtr)(&unicode),
                Attributes = 0x1040, // OBJ_DONT_REPARSE | OBJ_CASE_INSENSITIVE
                SecurityDescriptor = (IntPtr)security
            };
            var status = CreateRelative(out var raw, MaximumAllowed | 0x100000, ref attributes, out _, IntPtr.Zero, 0, ShareAll, create ? 2u : 1u, 0x200020u | (directory ? 1u : 0x40u), IntPtr.Zero, 0);
            if (!create && status is NameNotFound or PathNotFound or NoSuchFile)
            {
                return null;
            }

            if (status < 0)
            {
                throw Refused($"open {name} without following reparse points", status);
            }

            return new SafeFileHandle(raw, ownsHandle: true);
        }
    }

    internal static void CreateDirectory(SafeFileHandle parent, string name)
    {
        // The caller already attempted FILE_OPEN. A concurrent creation is refused rather than adopted silently.
        using var created = Open(parent, name, directory: true, create: true)!;
    }

    internal static KeyStoreFileIdentity Identity(SafeFileHandle handle)
    {
        if (!GetInformation(handle, out var information))
        {
            throw Refused("identify the pinned key handle");
        }

        if ((information.Attributes & 0x400) != 0)
        {
            throw new DevelopmentProfileRefused("the key store contains a link or reparse point; the profile never follows links.");
        }

        if (!GetIdentityInformation(handle, 18, out var identity, (uint)Marshal.SizeOf<FileIdentityInformation>()))
        {
            throw Refused("read the pinned key handle's 128-bit file identity");
        }

        var mode = (information.Attributes & 0x10) != 0 ? 0x4000 : 0x8000;
        return new KeyStoreFileIdentity(identity.Volume, identity.IndexLow, 0, mode, identity.IndexHigh);
    }

    internal static void RequireOwnerOnly(SafeFileHandle handle, string what)
    {
        var security = new FileSecurity();
        security.SetSecurityDescriptorBinaryForm(ReadSecurity(handle));
        WindowsOwnerOnlyAccess.RequireOwnerOnly(security, what);
        var descriptor = new RawSecurityDescriptor(security.GetSecurityDescriptorBinaryForm(), 0);
        if (!security.AreAccessRulesProtected || AccessControlPolicy.UnsafeAncestorReason(descriptor, CurrentUser()) is not null)
        {
            throw new DevelopmentProfileRefused($"{what} has an unsupported or unsafe access control list; remove it so it can be recreated.");
        }
    }

    internal static void RequireSafeAncestor(SafeFileHandle handle, string path)
    {
        var descriptor = new RawSecurityDescriptor(ReadSecurity(handle), 0);
        if (AccessControlPolicy.UnsafeAncestorReason(descriptor, CurrentUser()) is { } reason)
        {
            throw new DevelopmentProfileRefused($"the directory {path} above the key directory {reason}.");
        }
    }

    internal static unsafe IReadOnlyList<string> Entries(SafeFileHandle parent)
    {
        var names = new List<string>();
        var buffer = new byte[65536];
        // Custody serializes enumeration on this handle; restart the scan for every read/reload.
        fixed (byte* data = buffer)
        {
            var restart = true;
            while (true)
            {
                var status = QueryDirectory(parent, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, out _, data, (uint)buffer.Length, 1, 0, IntPtr.Zero, restart ? (byte)1 : (byte)0);
                if (status is NoMoreFiles or NoSuchFile)
                {
                    return names;
                }

                if (status < 0)
                {
                    throw Refused("enumerate the pinned key directory", status);
                }

                AppendEntries(data, names);
                restart = false;
            }
        }
    }

    internal static unsafe void Publish(SafeFileHandle parent, string name, SafeFileHandle file)
    {
        // FILE_RENAME_INFO, with ReplaceIfExists false and a relative RootDirectory.
        var rootOffset = IntPtr.Size;
        var lengthOffset = rootOffset + IntPtr.Size;
        var nameOffset = lengthOffset + sizeof(uint);
        var buffer = new byte[nameOffset + (name.Length * 2)];
        fixed (byte* data = buffer)
        fixed (char* characters = name)
        {
            *(IntPtr*)(data + rootOffset) = parent.DangerousGetHandle();
            *(uint*)(data + lengthOffset) = (uint)(name.Length * 2);
            Buffer.MemoryCopy(characters, data + nameOffset, name.Length * 2, name.Length * 2);
            if (!SetInformation(file, 3, data, (uint)buffer.Length))
            {
                throw Refused($"publish {name}");
            }
        }
    }

    internal static unsafe void Delete(SafeFileHandle parent, string name)
    {
        using var file = Open(parent, name, directory: false);
        if (file is null)
        {
            return;
        }

        uint delete = 1;
        if (!SetInformation(file, 4, (byte*)&delete, sizeof(uint)))
        {
            throw Refused($"remove {name}");
        }
    }

    static unsafe void AppendEntries(byte* data, List<string> names)
    {
        var offset = 0;
        while (true)
        {
            // FILE_DIRECTORY_INFORMATION: NextEntryOffset at 0, FileNameLength at 60, FileName at 64.
            var entry = data + offset;
            var name = new string((char*)(entry + 64), 0, checked((int)(*(uint*)(entry + 60) / 2)));
            if (name is not "." and not "..")
            {
                names.Add(name);
            }

            var next = *(uint*)entry;
            if (next == 0)
            {
                return;
            }

            offset += checked((int)next);
        }
    }

    static SecurityIdentifier CurrentUser() => WindowsIdentity.GetCurrent().User!;

    static byte[] OwnerOnlyDescriptor(bool directory)
    {
        var user = CurrentUser().Value;
        var inheritance = directory ? "OICI" : string.Empty;
        var descriptor = new RawSecurityDescriptor($"O:{user}D:P(A;{inheritance};FA;;;{user})");
        var bytes = new byte[descriptor.BinaryLength];
        descriptor.GetBinaryForm(bytes, 0);
        return bytes;
    }

    static byte[] ReadSecurity(SafeFileHandle handle)
    {
        if (GetSecurity(handle, 1, 7, out _, out _, out _, out _, out var descriptor) != 0)
        {
            throw new DevelopmentProfileRefused("the access control of a pinned key store handle cannot be read, so the store cannot be shown safe.");
        }

        try
        {
            var bytes = new byte[GetSecurityLength(descriptor)];
            Marshal.Copy(descriptor, bytes, 0, bytes.Length);
            return bytes;
        }
        finally
        {
            _ = FreeLocal(descriptor);
        }
    }

    static DevelopmentProfileRefused Refused(string operation, int? status = null) => new($"cannot {operation} in the development key store (system error {status ?? Marshal.GetLastPInvokeError()}); no key material was adopted.");

    [StructLayout(LayoutKind.Sequential)]
    struct UnicodeName
    {
        internal ushort Length;
        internal ushort MaximumLength;
        internal IntPtr Buffer;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct ObjectAttributes
    {
        internal uint Length;
        internal IntPtr RootDirectory;
        internal IntPtr ObjectName;
        internal uint Attributes;
        internal IntPtr SecurityDescriptor;
        internal IntPtr SecurityQualityOfService;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct IoStatus
    {
        internal IntPtr Status;
        internal UIntPtr Information;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct FileIdentityInformation
    {
        internal ulong Volume;
        internal ulong IndexLow;
        internal ulong IndexHigh;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct FileInformation
    {
        internal uint Attributes;
        internal uint CreationLow;
        internal uint CreationHigh;
        internal uint AccessLow;
        internal uint AccessHigh;
        internal uint WriteLow;
        internal uint WriteHigh;
        internal uint Volume;
        internal uint SizeHigh;
        internal uint SizeLow;
        internal uint Links;
        internal uint IndexHigh;
        internal uint IndexLow;
    }

    [LibraryImport("kernel32.dll", EntryPoint = "CreateFileW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial SafeFileHandle CreateFile(string path, uint access, uint share, IntPtr security, uint disposition, uint flags, IntPtr template);

    [LibraryImport("ntdll.dll", EntryPoint = "NtCreateFile")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int CreateRelative(out IntPtr file, uint access, ref ObjectAttributes attributes, out IoStatus status, IntPtr allocation, uint fileAttributes, uint share, uint disposition, uint options, IntPtr extendedAttributes, uint extendedAttributesLength);

    [LibraryImport("kernel32.dll", EntryPoint = "GetFileInformationByHandle", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetInformation(SafeFileHandle file, out FileInformation information);

    [LibraryImport("kernel32.dll", EntryPoint = "GetFileInformationByHandleEx", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetIdentityInformation(SafeFileHandle file, int informationClass, out FileIdentityInformation information, uint length);

    [LibraryImport("ntdll.dll", EntryPoint = "NtQueryDirectoryFile")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static unsafe partial int QueryDirectory(SafeFileHandle directory, IntPtr eventHandle, IntPtr routine, IntPtr context, out IoStatus status, byte* buffer, uint length, int informationClass, byte singleEntry, IntPtr name, byte restart);

    [LibraryImport("kernel32.dll", EntryPoint = "SetFileInformationByHandle", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static unsafe partial bool SetInformation(SafeFileHandle file, int informationClass, byte* information, uint length);

    [LibraryImport("advapi32.dll", EntryPoint = "GetSecurityInfo")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial uint GetSecurity(SafeFileHandle file, int objectType, uint information, out IntPtr owner, out IntPtr group, out IntPtr dacl, out IntPtr sacl, out IntPtr descriptor);

    [LibraryImport("advapi32.dll", EntryPoint = "GetSecurityDescriptorLength")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial uint GetSecurityLength(IntPtr descriptor);

    [LibraryImport("kernel32.dll", EntryPoint = "LocalFree")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial IntPtr FreeLocal(IntPtr memory);
}

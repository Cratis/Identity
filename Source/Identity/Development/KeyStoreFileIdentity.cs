// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Development;

/// <summary>
/// The identity and security-relevant metadata of an open object, never metadata obtained by resolving its pathname.
/// </summary>
/// <param name="Device">The POSIX device or Windows volume identifier.</param>
/// <param name="Inode">The inode or Windows file identifier.</param>
/// <param name="Owner">The POSIX owner user ID.</param>
/// <param name="Mode">The POSIX mode, or equivalent object type on Windows.</param>
/// <param name="InodeHigh">The high half of a Windows 128-bit file identifier.</param>
internal sealed record KeyStoreFileIdentity(ulong Device, ulong Inode, uint Owner, int Mode, ulong InodeHigh = 0)
{
    internal bool IsDirectory => (Mode & 0xf000) == 0x4000;

    internal bool IsFile => (Mode & 0xf000) == 0x8000;

    internal bool SameObject(KeyStoreFileIdentity other) => Device == other.Device && Inode == other.Inode && InodeHigh == other.InodeHigh;
}

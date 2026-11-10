// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Cratis.Identity.Development;

/// <summary>
/// The decisions the Windows key store checks make about a raw security descriptor, kept apart from the calls that read
/// one so they can be exercised without touching the file system.
/// </summary>
[SupportedOSPlatform("windows")]
static class AccessControlPolicy
{
    /// <summary>
    /// Delete, DeleteSubdirectoriesAndFiles, ChangePermissions, TakeOwnership and the unmapped GenericAll: any of these on a
    /// parent lets the holder replace or take over the directory below it.
    /// </summary>
    const int ReplaceRights = 0x10000 | 0x40 | 0x40000 | 0x80000 | 0x10000000;

    static readonly SecurityIdentifier _system = new(WellKnownSidType.LocalSystemSid, null);
    static readonly SecurityIdentifier _administrators = new(WellKnownSidType.BuiltinAdministratorsSid, null);
    static readonly SecurityIdentifier _creatorOwner = new(WellKnownSidType.CreatorOwnerSid, null);
    static readonly SecurityIdentifier _trustedInstaller = new("S-1-5-80-956008885-3418522649-1831038044-1853292631-2271478464");

    /// <summary>
    /// Gets whether the descriptor carries a discretionary access control list. A descriptor with none (a NULL DACL) grants
    /// everyone full access, so it is never owner-only.
    /// </summary>
    /// <param name="descriptor">The <see cref="RawSecurityDescriptor"/>.</param>
    /// <returns><see langword="true"/> when a discretionary access control list is present.</returns>
    public static bool HasDiscretionaryAcl(RawSecurityDescriptor descriptor) =>
        descriptor.ControlFlags.HasFlag(ControlFlags.DiscretionaryAclPresent) && descriptor.DiscretionaryAcl is not null;

    /// <summary>
    /// Describes why a directory above the key store could let another principal replace it, or returns null when it cannot.
    /// </summary>
    /// <param name="descriptor">The <see cref="RawSecurityDescriptor"/> of the ancestor.</param>
    /// <param name="user">The current user.</param>
    /// <returns>The reason, or <see langword="null"/> when the ancestor is safe.</returns>
    public static string? UnsafeAncestorReason(RawSecurityDescriptor descriptor, SecurityIdentifier user)
    {
        if (descriptor.Owner is null || !IsTrusted(descriptor.Owner, user))
        {
            return "is owned by another principal";
        }

        if (!HasDiscretionaryAcl(descriptor))
        {
            return "has no access control list, so everyone can replace the key store";
        }

        foreach (var entry in descriptor.DiscretionaryAcl!)
        {
            // Conditional/callback and object-specific ACEs cannot be evaluated here. Never silently omit them.
            if (entry is not CommonAce ace || !IsSupported(ace))
            {
                return "has an unsupported access control entry, so replacement cannot be ruled out";
            }

            if (AllowsReplacement(ace) && !IsTrusted(ace.SecurityIdentifier, user))
            {
                return $"lets {ace.SecurityIdentifier.Value} delete or take over what it contains";
            }
        }

        return null;
    }

    static bool IsSupported(CommonAce ace) =>
        !ace.IsCallback && ace.AceQualifier is AceQualifier.AccessAllowed or AceQualifier.AccessDenied;

    static bool AllowsReplacement(CommonAce ace) =>
        ace.AceQualifier == AceQualifier.AccessAllowed &&
        !ace.AceFlags.HasFlag(AceFlags.InheritOnly) &&
        (ace.AccessMask & ReplaceRights) != 0;

    static bool IsTrusted(SecurityIdentifier sid, SecurityIdentifier user) =>
        sid.Equals(user) || sid.Equals(_system) || sid.Equals(_administrators) || sid.Equals(_creatorOwner) || sid.Equals(_trustedInstaller);
}

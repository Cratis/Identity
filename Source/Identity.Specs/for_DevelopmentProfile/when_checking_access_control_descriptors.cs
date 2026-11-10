// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using Cratis.Identity.Development;

namespace Cratis.Identity.for_DevelopmentProfile;

/// <summary>
/// The security descriptor types are Windows-only, so the observations are made on Windows and the specs hold by default elsewhere.
/// </summary>
public class when_checking_access_control_descriptors
{
    bool _nullDacl;
    bool _emptyDacl = true;
    string? _ownedByOther = "skipped";
    string? _otherMayDelete = "skipped";
    string? _otherMayOnlyRead;
    string? _userOnly;
    string? _missingDacl = "skipped";

    void Because()
    {
        if (OperatingSystem.IsWindows())
        {
            Inspect();
        }
    }

    [Fact] void should_not_treat_a_null_dacl_as_owner_only() => _nullDacl.ShouldBeFalse();
    [Fact] void should_accept_an_empty_dacl() => _emptyDacl.ShouldBeTrue();
    [Fact] void should_refuse_an_ancestor_owned_by_another_principal() => _ownedByOther.ShouldNotBeNull();
    [Fact] void should_refuse_an_ancestor_another_principal_can_delete_from() => _otherMayDelete.ShouldNotBeNull();
    [Fact] void should_accept_an_ancestor_another_principal_can_only_read() => _otherMayOnlyRead.ShouldBeNull();
    [Fact] void should_accept_an_ancestor_only_the_user_controls() => _userOnly.ShouldBeNull();
    [Fact] void should_refuse_an_ancestor_with_no_dacl() => _missingDacl.ShouldNotBeNull();

    [SupportedOSPlatform("windows")]
    static RawSecurityDescriptor Descriptor(SecurityIdentifier owner, params (SecurityIdentifier Sid, int Mask)[] allow)
    {
        var acl = new RawAcl(GenericAcl.AclRevision, allow.Length);
        foreach (var (sid, mask) in allow)
        {
            acl.InsertAce(acl.Count, new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, mask, sid, false, null));
        }

        return new RawSecurityDescriptor(ControlFlags.DiscretionaryAclPresent, owner, null, null, acl);
    }

    [SupportedOSPlatform("windows")]
    void Inspect()
    {
        var user = new SecurityIdentifier("S-1-5-21-1-2-3-1001");
        var other = new SecurityIdentifier("S-1-5-21-1-2-3-1002");
        _nullDacl = AccessControlPolicy.HasDiscretionaryAcl(new RawSecurityDescriptor("O:S-1-5-21-1-2-3-1001"));
        _emptyDacl = AccessControlPolicy.HasDiscretionaryAcl(new RawSecurityDescriptor("O:S-1-5-21-1-2-3-1001D:"));
        _ownedByOther = AccessControlPolicy.UnsafeAncestorReason(Descriptor(other, (user, 0x1F01FF)), user);
        _otherMayDelete = AccessControlPolicy.UnsafeAncestorReason(Descriptor(user, (user, 0x1F01FF), (other, 0x40)), user);
        _otherMayOnlyRead = AccessControlPolicy.UnsafeAncestorReason(Descriptor(user, (user, 0x1F01FF), (other, 0x1200A9)), user);
        _userOnly = AccessControlPolicy.UnsafeAncestorReason(Descriptor(user, (user, 0x1F01FF)), user);
        _missingDacl = AccessControlPolicy.UnsafeAncestorReason(new RawSecurityDescriptor("O:S-1-5-21-1-2-3-1001"), user);
    }
}

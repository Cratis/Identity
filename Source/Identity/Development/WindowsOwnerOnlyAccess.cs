// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using Cratis.Identity.Hosting;

namespace Cratis.Identity.Development;

/// <summary>
/// Creates and verifies owner-only access control lists for the development key store on Windows. The access control
/// list is part of the creation call, so there is no window in which a new directory or file is readable by others.
/// </summary>
[SupportedOSPlatform("windows")]
static class WindowsOwnerOnlyAccess
{
    /// <summary>
    /// Creates a directory whose only access is full control for the current user, with inheritance from its parent disabled.
    /// </summary>
    /// <param name="path">The directory to create.</param>
    public static void CreateDirectory(string path)
    {
        var user = CurrentUser();
        var security = new DirectorySecurity();
        security.SetOwner(user);
        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
        security.AddAccessRule(new FileSystemAccessRule(user, FileSystemRights.FullControl, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
        security.CreateDirectory(path);
    }

    /// <summary>
    /// Creates a new file, failing when it exists, whose only access is full control for the current user, and writes the content.
    /// </summary>
    /// <param name="path">The file to create.</param>
    /// <param name="content">The text to write.</param>
    public static void CreateFile(string path, string content)
    {
        var user = CurrentUser();
        var security = new FileSecurity();
        security.SetOwner(user);
        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
        security.AddAccessRule(new FileSystemAccessRule(user, FileSystemRights.FullControl, AccessControlType.Allow));
        using var stream = new FileInfo(path).Create(FileMode.CreateNew, FileSystemRights.FullControl, FileShare.None, 4096, FileOptions.WriteThrough, security);
        stream.Write(Encoding.UTF8.GetBytes(content));
        stream.Flush(flushToDisk: true);
    }

    /// <summary>
    /// Refuses a security descriptor that is owned by someone else or grants access to anyone but the current user.
    /// </summary>
    /// <param name="security">The <see cref="FileSystemSecurity"/> of the directory or file.</param>
    /// <param name="what">What is checked, for the message.</param>
    /// <exception cref="DevelopmentProfileRefused">Thrown when the access control list is not owner-only.</exception>
    public static void RequireOwnerOnly(FileSystemSecurity security, string what)
    {
        var user = CurrentUser();
        if (!AccessControlPolicy.HasDiscretionaryAcl(new RawSecurityDescriptor(security.GetSecurityDescriptorBinaryForm(), 0)))
        {
            throw new DevelopmentProfileRefused($"{what} has no access control list, which grants everyone access; remove it so it can be recreated.");
        }

        var processOwner = WindowsIdentity.GetCurrent().Owner ?? user;
        if (security.GetOwner(typeof(SecurityIdentifier)) is not SecurityIdentifier owner || (!owner.Equals(user) && !owner.Equals(processOwner)))
        {
            throw new DevelopmentProfileRefused($"{what} is not owned by the current user; remove it so it can be recreated.");
        }

        foreach (FileSystemAccessRule rule in security.GetAccessRules(includeExplicit: true, includeInherited: true, typeof(SecurityIdentifier)))
        {
            if (rule.AccessControlType == AccessControlType.Allow && !rule.IdentityReference.Equals(user))
            {
                throw new DevelopmentProfileRefused($"{what} grants access to other users; remove it so it can be recreated with an owner-only access control list.");
            }
        }
    }

    /// <summary>
    /// Refuses a directory above the key store that another principal could use to replace it.
    /// </summary>
    /// <param name="ancestor">The ancestor directory.</param>
    /// <exception cref="DevelopmentProfileRefused">Thrown when the ancestor cannot be shown safe.</exception>
    public static void RequireSafeAncestor(string ancestor)
    {
        try
        {
            var descriptor = new RawSecurityDescriptor(new DirectoryInfo(ancestor).GetAccessControl().GetSecurityDescriptorBinaryForm(), 0);
            if (AccessControlPolicy.UnsafeAncestorReason(descriptor, CurrentUser()) is { } reason)
            {
                throw new DevelopmentProfileRefused($"the directory {ancestor} above the key directory {reason}.");
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or InvalidOperationException)
        {
            throw new DevelopmentProfileRefused($"the access control of {ancestor} above the key directory cannot be read, so the store cannot be shown safe from replacement.");
        }
    }

    static SecurityIdentifier CurrentUser() => WindowsIdentity.GetCurrent().User!;
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Formats.Tar;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Cratis.Identity.Hosting;

namespace Cratis.Identity.Development;

/// <summary>
/// The dedicated key store of the local-development profile: one fixed per-user directory that holds only keys this
/// profile created, so a development token can never be signed with, or verified against, deployed key material.
/// </summary>
/// <remarks>
/// <para>
/// The directory is created with owner-only permissions and every file is created owner-only and moved into place
/// atomically; nothing is changed after the fact. On Unix that is a creation mode; on Windows it is an owner-only access
/// control list passed to the creation call, with inheritance disabled.
/// </para>
/// <para>
/// A pre-existing directory is used only when it carries the development provenance marker, holds nothing but development
/// key files and development certificates, is owned by the current user, is neither a link nor reached through one, and none of
/// its ancestors can be replaced by another user. Files are verified on the handle they are read through: the mode on Unix,
/// the access control list (which must exist) on Windows. Residual risk: .NET has no no-follow open and no owner-of-handle
/// call on Unix, so links are refused before and after the open and the owner is read by path; exploiting that gap needs
/// write access to the owner-only directory, that is the same user.
/// A partial write left by a crash is refused for a temporary file and cleaned for a key without its certificate.
/// Anything else is refused, so a deployed key ring or a link into a repository is never adopted.
/// </para>
/// </remarks>
/// <param name="root">The per-user data root, normally the local application data folder.</param>
/// <param name="contentRoot">The content root of the host; the store is refused inside it.</param>
/// <param name="timeProvider">The <see cref="TimeProvider"/>.</param>
internal sealed class DevelopmentKeyStore(string root, string contentRoot, TimeProvider timeProvider)
{
    /// <summary>
    /// The provenance marker file, written when the directory is created. A dot entry, so the key loader ignores it.
    /// </summary>
    internal const string MarkerFile = ".cratis-identity-development";

    /// <summary>
    /// The content of the provenance marker file.
    /// </summary>
    internal const string Marker = "cratis-identity local-development key store. Never copy, deploy or trust.";

    /// <summary>
    /// The subject of every development certificate.
    /// </summary>
    internal const string Subject = "CN=cratis-identity-local-dev, OU=NOT-FOR-DEPLOYMENT";

    /// <summary>
    /// The prefix of every development key name, so a development key is recognizable in a key identifier.
    /// </summary>
    internal const string NamePrefix = "dev-";

    /// <summary>
    /// The name of the directory below the store that holds the Data Protection key ring ASP.NET Core generates itself.
    /// </summary>
    internal const string DataProtectionDirectory = "data-protection";

    /// <summary>
    /// The prefix of the temporary file an atomic write goes through.
    /// </summary>
    internal const string TemporaryPrefix = ".tmp-";

    const int KeySize = 3072;
    const UnixFileMode OwnerDirectory = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;
    const UnixFileMode OwnerFile = UnixFileMode.UserRead | UnixFileMode.UserWrite;
    static readonly TimeSpan _validity = TimeSpan.FromDays(365);
    static readonly TimeSpan _renewBefore = TimeSpan.FromDays(7);
    static readonly string[] _purposes = ["signing", "encryption"];
    static readonly Lazy<int> _currentUid = new(ProbeCurrentUid);

    /// <summary>
    /// Gets the default per-user data root.
    /// </summary>
    public static string DefaultRoot => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

    /// <summary>
    /// Gets the path comparison for the current file system: case-insensitive on Windows and macOS, where a path that
    /// differs only in case names the same directory, and ordinal elsewhere.
    /// </summary>
    internal static StringComparison PathComparison => OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    /// <summary>
    /// Creates the store directory when it does not exist, refuses it when it is not safe, and makes sure it holds a
    /// currently valid signing key and encryption key.
    /// </summary>
    /// <param name="authority">The sanitized issuer authority that names the directory.</param>
    /// <returns>The directory the keys are in.</returns>
    /// <exception cref="DevelopmentProfileRefused">Thrown when the location or the existing content is not safe.</exception>
    public string Provision(string authority)
    {
        var directory = ResolveLocation(root, contentRoot, authority);
        RequireSafeAncestors(directory);
        EnsureDirectory(directory);
        var content = CheckExistingContent(directory);
        RemoveOrphanKeys(directory, content);
        EnsureMarker(directory);
        EnsureDirectory(Path.Combine(directory, DataProtectionDirectory));

        var now = timeProvider.GetUtcNow();
        var valid = content.Certificates.Where(_ => _.NotBefore <= now && now < _.NotAfter - _renewBefore).Select(_ => _.Purpose).ToHashSet();
        var taken = content.Certificates.Select(_ => _.Name).Concat(content.KeyNames).ToHashSet(StringComparer.Ordinal);
        foreach (var purpose in _purposes.Where(_ => !valid.Contains(_)))
        {
            Create(directory, purpose, NewName(taken, now), now);
        }

        return directory;
    }

    /// <summary>
    /// Gets whether a path is the parent or lies below it, compared on canonical full paths with the file system's case rules.
    /// </summary>
    /// <param name="path">The path to test.</param>
    /// <param name="parent">The parent.</param>
    /// <returns><see langword="true"/> when the path equals the parent or is inside it.</returns>
    internal static bool IsUnderOrEqual(string path, string parent)
    {
        var normalizedPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
        var normalizedParent = Path.TrimEndingDirectorySeparator(Path.GetFullPath(parent));
        return string.Equals(normalizedPath, normalizedParent, PathComparison) ||
            normalizedPath.StartsWith(normalizedParent + Path.DirectorySeparatorChar, PathComparison);
    }

    static string Resolve(string path)
    {
        var full = Path.GetFullPath(path);
        var current = Path.GetPathRoot(full)!;
        foreach (var segment in full[current.Length..].Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, segment);
            var info = new DirectoryInfo(current);
            if (info.LinkTarget is null)
            {
                continue;
            }

            try
            {
                current = info.ResolveLinkTarget(returnFinalTarget: true)!.FullName;
            }
            catch (IOException)
            {
                throw new DevelopmentProfileRefused("the key directory is reached through a link that cannot be resolved; the profile never follows links.");
            }
        }

        return current;
    }

    static string NewName(HashSet<string> taken, DateTimeOffset now)
    {
        var baseName = $"{NamePrefix}{now:yyyyMMdd}";
        var name = baseName;
        for (var counter = 2; taken.Contains(name); counter++)
        {
            name = $"{baseName}-{counter}";
        }

        taken.Add(name);
        return name;
    }

    static void CreateDirectory(string directory)
    {
        if (OperatingSystem.IsWindows())
        {
            WindowsOwnerOnlyAccess.CreateDirectory(directory);
        }
        else
        {
            Directory.CreateDirectory(directory, OwnerDirectory);
        }
    }

    static void WriteAtomically(string directory, string file, string content)
    {
        var temporary = Path.Combine(directory, $"{TemporaryPrefix}{Guid.NewGuid():N}");
        try
        {
            if (OperatingSystem.IsWindows())
            {
                WindowsOwnerOnlyAccess.CreateFile(temporary, content);
            }
            else
            {
                // CreateNew is O_EXCL: it never follows a link and fails when the name exists.
                var options = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write, Share = FileShare.None, UnixCreateMode = OwnerFile };
                using var stream = new FileStream(temporary, options);
                if (OperatingSystem.IsMacOS())
                {
                    MacExtendedAccess.RequireNoGrants(stream.SafeFileHandle);
                }

                stream.Write(Encoding.UTF8.GetBytes(content));
                stream.Flush(flushToDisk: true);
            }

            File.Move(temporary, Path.Combine(directory, file), overwrite: false);
        }
        finally
        {
            File.Delete(temporary);
        }
    }

    static int ProbeCurrentUid()
    {
        if (OperatingSystem.IsWindows())
        {
            return -1;
        }

        var probe = Path.Combine(Path.GetTempPath(), $"cratis-identity-uid-{Guid.NewGuid():N}");
        try
        {
            using (new FileStream(probe, new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write, UnixCreateMode = OwnerFile }))
            {
            }

            return OwnerOf(probe);
        }
        finally
        {
            File.Delete(probe);
        }
    }

    /// <summary>
    /// The owner of a path on Unix, read through the tar writer, which records the file system's user id.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns>The user id of the owner.</returns>
    static int OwnerOf(string path)
    {
        using var buffer = new MemoryStream();
        using (var writer = new TarWriter(buffer, TarEntryFormat.Pax, leaveOpen: true))
        {
            writer.WriteEntry(path, "entry");
        }

        buffer.Position = 0;
        using var reader = new TarReader(buffer);
        return reader.GetNextEntry()!.Uid;
    }

    static void RequireOwnedByCurrentUser(string path, string what)
    {
        if (OwnerOf(path) != _currentUid.Value)
        {
            throw new DevelopmentProfileRefused($"{what} is not owned by the current user; remove it so it can be recreated.");
        }
    }

    static void RequireNotLink(string path)
    {
        if (IsLink(path))
        {
            throw new DevelopmentProfileRefused($"{Path.GetFileName(path)} is a link or reparse point; the profile never follows links.");
        }
    }

    static bool IsLink(string path) =>
        new FileInfo(path).LinkTarget is not null || File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint);

    static void RequireOwnerOnly(string directory)
    {
        RequireNotLink(directory);
        if (OperatingSystem.IsWindows())
        {
            WindowsOwnerOnlyAccess.RequireOwnerOnly(new DirectoryInfo(directory).GetAccessControl(), "the key directory");
            return;
        }

        if ((File.GetUnixFileMode(directory) & ~OwnerDirectory) != UnixFileMode.None)
        {
            throw new DevelopmentProfileRefused("the key directory is accessible to other users; remove it so it can be recreated with owner-only permissions.");
        }

        if (OperatingSystem.IsMacOS())
        {
            MacExtendedAccess.RequireNoGrants(directory);
        }

        RequireOwnedByCurrentUser(directory, "the key directory");
    }

    /// <summary>
    /// On Unix another user who can write to an ancestor can swap the store for a link or a copy, so every ancestor must be
    /// owned by the current user or root and not writable by others (a sticky directory excepted). On Windows every ancestor must
    /// be owned by the current user, SYSTEM, Administrators or TrustedInstaller and grant nobody else the right to delete or take
    /// over what it contains; an access control list that cannot be read is refused.
    /// </summary>
    /// <param name="directory">The store directory.</param>
    /// <exception cref="DevelopmentProfileRefused">Thrown when an ancestor is writable by others or owned by another user.</exception>
    static void RequireSafeAncestors(string directory)
    {
        if (OperatingSystem.IsWindows())
        {
            for (var ancestor = Path.GetDirectoryName(directory); ancestor is not null; ancestor = Path.GetDirectoryName(ancestor))
            {
                if (Directory.Exists(ancestor))
                {
                    WindowsOwnerOnlyAccess.RequireSafeAncestor(ancestor);
                }
            }

            return;
        }

        for (var ancestor = Path.GetDirectoryName(directory); ancestor is not null; ancestor = Path.GetDirectoryName(ancestor))
        {
            if (Directory.Exists(ancestor))
            {
                RequireSafeUnixAncestor(ancestor);
            }
        }
    }

    static void RequireSafeUnixAncestor(string ancestor)
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        if (OperatingSystem.IsMacOS())
        {
            MacExtendedAccess.RequireNoGrants(ancestor);
        }

        var mode = File.GetUnixFileMode(ancestor);
        var writableByOthers = (mode & (UnixFileMode.GroupWrite | UnixFileMode.OtherWrite)) != UnixFileMode.None;
        if (writableByOthers && !mode.HasFlag(UnixFileMode.StickyBit))
        {
            throw new DevelopmentProfileRefused($"the directory {ancestor} above the key directory is writable by other users, who could replace the key store.");
        }

        var owner = OwnerOf(ancestor);
        if (owner != _currentUid.Value && owner != 0)
        {
            throw new DevelopmentProfileRefused($"the directory {ancestor} above the key directory is owned by another user, who could replace the key store.");
        }
    }

    static bool IsDevelopmentFile(string name)
    {
        var parts = name.Split('.');
        return parts.Length == 3 && _purposes.Contains(parts[0], StringComparer.Ordinal) &&
            parts[1].StartsWith(NamePrefix, StringComparison.Ordinal) && parts[1].All(_ => char.IsAsciiLetterOrDigit(_) || _ == '-') &&
            (parts[2] == "crt" || parts[2] == "key");
    }

    static string ResolveLocation(string root, string contentRoot, string authority)
    {
        if (string.IsNullOrWhiteSpace(root))
        {
            throw new DevelopmentProfileRefused("the per-user application data folder is not available on this machine.");
        }

        var expected = Path.Combine(Resolve(root), "cratis", "identity-dev", authority);
        if (!string.Equals(Resolve(expected), expected, PathComparison))
        {
            throw new DevelopmentProfileRefused("the key directory is a symbolic link or is reached through one; the profile never follows links.");
        }

        if (IsUnderOrEqual(expected, Resolve(contentRoot)))
        {
            throw new DevelopmentProfileRefused("the key directory is inside the application content root, where it could be committed.");
        }

        return expected;
    }

    static void EnsureDirectory(string directory)
    {
        if (!Directory.Exists(directory))
        {
            CreateDirectory(directory);
        }

        RequireOwnerOnly(directory);
    }

    static ExistingContent CheckExistingContent(string directory)
    {
        var entries = Directory.EnumerateFileSystemEntries(directory).ToList();
        var marker = Path.Combine(directory, MarkerFile);
        if (entries.Count > 0 && !File.Exists(marker))
        {
            throw new DevelopmentProfileRefused("the key directory already holds material that is not marked as development keys (for example a deployed key ring); it is never adopted.");
        }

        if (File.Exists(marker))
        {
            VerifyMarker(marker);
        }

        var content = new ExistingContent();
        foreach (var entry in entries.Where(_ => !string.Equals(Path.GetFileName(_), MarkerFile, StringComparison.Ordinal)))
        {
            CheckEntry(entry, content);
        }

        return content;
    }

    static void VerifyMarker(string marker)
    {
        RequireNotLink(marker);
        using var stream = OpenVerified(marker);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        if (!string.Equals(reader.ReadToEnd().Trim(), Marker, StringComparison.Ordinal))
        {
            throw new DevelopmentProfileRefused("the development provenance marker is not valid.");
        }
    }

    static void CheckEntry(string entry, ExistingContent content)
    {
        var name = Path.GetFileName(entry);
        if (name.StartsWith(TemporaryPrefix, StringComparison.Ordinal))
        {
            throw new DevelopmentProfileRefused($"the key directory holds a partial write ({name}) left by an earlier run; remove the key directory so it can be recreated.");
        }

        var isDirectory = Directory.Exists(entry);
        var expected = isDirectory ? name == DataProtectionDirectory : IsDevelopmentFile(name);
        if (IsLink(entry) || !expected)
        {
            throw new DevelopmentProfileRefused($"the key directory holds an unexpected entry ({name}); only development key files are allowed.");
        }

        if (isDirectory)
        {
            RequireOwnerOnly(entry);
            return;
        }

        using var stream = OpenVerified(entry);
        if (name.EndsWith(".crt", StringComparison.Ordinal))
        {
            content.Certificates.Add(ReadCertificate(name, stream));
        }
        else
        {
            content.KeyNames.Add(name.Split('.')[1]);
            content.Keys.Add(name);
        }
    }

    /// <summary>
    /// Opens the file and verifies its mode (Unix) or access control list (Windows) on that same handle, so what is checked is what is read.
    /// </summary>
    /// <param name="file">The file.</param>
    /// <returns>The open, verified stream.</returns>
    /// <exception cref="DevelopmentProfileRefused">Thrown when the file is accessible to other users or owned by another user.</exception>
    static FileStream OpenVerified(string file)
    {
        RequireNotLink(file);
        var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
        try
        {
            // .NET exposes no no-follow open, so a swap between the check above and the open is detected here, and the
            // directory is owner-only below verified ancestors, so only the same user could make one.
            RequireNotLink(file);
            if (OperatingSystem.IsWindows())
            {
                WindowsOwnerOnlyAccess.RequireOwnerOnly(stream.GetAccessControl(), $"the key file {Path.GetFileName(file)}");
            }
            else
            {
                if ((File.GetUnixFileMode(stream.SafeFileHandle) & ~OwnerFile) != UnixFileMode.None)
                {
                    throw new DevelopmentProfileRefused($"the key file {Path.GetFileName(file)} is accessible to other users; remove the key directory so it can be recreated.");
                }

                if (OperatingSystem.IsMacOS())
                {
                    MacExtendedAccess.RequireNoGrants(stream.SafeFileHandle);
                }

                // .NET has no public owner-of-a-handle call; the owner is read by path straight after the mode check on the handle.
                RequireOwnedByCurrentUser(file, $"the key file {Path.GetFileName(file)}");
            }

            return stream;
        }
        catch
        {
            stream.Dispose();
            throw;
        }
    }

    static DevelopmentCertificate ReadCertificate(string name, FileStream stream)
    {
        try
        {
            using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
            using var certificate = X509Certificate2.CreateFromPem(reader.ReadToEnd());
            if (!string.Equals(certificate.SubjectName.Name, new X500DistinguishedName(Subject).Name, StringComparison.Ordinal))
            {
                throw new DevelopmentProfileRefused($"the certificate {name} is not a development certificate.");
            }

            var parts = name.Split('.');
            return new DevelopmentCertificate(parts[0], parts[1], new DateTimeOffset(certificate.NotBefore.ToUniversalTime()), new DateTimeOffset(certificate.NotAfter.ToUniversalTime()));
        }
        catch (CryptographicException)
        {
            throw new DevelopmentProfileRefused($"the certificate {name} is malformed.");
        }
    }

    /// <summary>
    /// A key without its certificate is what a crash between the two writes leaves; the key loader would not use it, so remove it.
    /// A certificate without its key cannot be used either, and is never repaired silently.
    /// </summary>
    /// <param name="directory">The store directory.</param>
    /// <param name="content">The existing content.</param>
    /// <exception cref="DevelopmentProfileRefused">Thrown when a certificate has no private key.</exception>
    static void RemoveOrphanKeys(string directory, ExistingContent content)
    {
        var certificates = content.Certificates.Select(_ => $"{_.Purpose}.{_.Name}.key").ToHashSet(StringComparer.Ordinal);
        var missing = certificates.FirstOrDefault(_ => !content.Keys.Contains(_));
        if (missing is not null)
        {
            throw new DevelopmentProfileRefused($"the certificate for {missing} has no private key; remove the key directory so it can be recreated.");
        }

        foreach (var key in content.Keys.Where(_ => !certificates.Contains(_)))
        {
            File.Delete(Path.Combine(directory, key));
        }
    }

    static void EnsureMarker(string directory)
    {
        if (!File.Exists(Path.Combine(directory, MarkerFile)))
        {
            WriteAtomically(directory, MarkerFile, Marker);
        }
    }

    static void Create(string directory, string purpose, string name, DateTimeOffset now)
    {
        using var rsa = RSA.Create(KeySize);
        var request = new CertificateRequest(Subject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(now.AddMinutes(-1), now.Add(_validity));

        // The key first, then the certificate: each file is complete the moment it is visible.
        WriteAtomically(directory, $"{purpose}.{name}.key", rsa.ExportPkcs8PrivateKeyPem());
        WriteAtomically(directory, $"{purpose}.{name}.crt", certificate.ExportCertificatePem());
    }

    sealed record DevelopmentCertificate(string Purpose, string Name, DateTimeOffset NotBefore, DateTimeOffset NotAfter);

    sealed class ExistingContent
    {
        public List<DevelopmentCertificate> Certificates { get; } = [];

        public HashSet<string> Keys { get; } = new(StringComparer.Ordinal);

        public HashSet<string> KeyNames { get; } = new(StringComparer.Ordinal);
    }
}

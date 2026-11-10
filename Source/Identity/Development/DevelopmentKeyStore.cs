// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
/// The directory is created with owner-only permissions and every file is created owner-only and moved into place
/// atomically; nothing is changed after the fact. A pre-existing directory is used only when it carries the development
/// provenance marker, holds nothing but development key files and development certificates, and is neither a link nor
/// reached through one. Anything else is refused, so a deployed key ring or a link into a repository is never adopted.
/// On Windows the directory inherits the owner-only access control of the user's local application data folder.
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

    const int KeySize = 3072;
    const UnixFileMode OwnerDirectory = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;
    const UnixFileMode OwnerFile = UnixFileMode.UserRead | UnixFileMode.UserWrite;
    static readonly TimeSpan _validity = TimeSpan.FromDays(365);
    static readonly TimeSpan _renewBefore = TimeSpan.FromDays(7);
    static readonly string[] _purposes = ["signing", "encryption"];

    /// <summary>
    /// Gets the default per-user data root.
    /// </summary>
    public static string DefaultRoot => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

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
        EnsureDirectory(directory);
        var files = CheckExistingContent(directory);
        EnsureMarker(directory);
        EnsureDirectory(Path.Combine(directory, DataProtectionDirectory));

        var now = timeProvider.GetUtcNow();
        var valid = files.Where(_ => _.NotBefore <= now && now < _.NotAfter - _renewBefore).Select(_ => _.Purpose).ToHashSet();
        var taken = files.Select(_ => _.Name).ToHashSet();
        foreach (var purpose in _purposes.Where(_ => !valid.Contains(_)))
        {
            Create(directory, purpose, NewName(taken, now), now);
        }

        return directory;
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

    static bool IsUnder(string path, string parent) =>
        path.StartsWith(parent.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.Ordinal);

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
            Directory.CreateDirectory(directory);
        }
        else
        {
            Directory.CreateDirectory(directory, OwnerDirectory);
        }
    }

    static void WriteAtomically(string directory, string file, string content)
    {
        var temporary = Path.Combine(directory, $".tmp-{Guid.NewGuid():N}");
        var options = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write, Share = FileShare.None };
        if (!OperatingSystem.IsWindows())
        {
            options.UnixCreateMode = OwnerFile;
        }

        try
        {
            using (var stream = new FileStream(temporary, options))
            {
                stream.Write(Encoding.UTF8.GetBytes(content));
            }

            File.Move(temporary, Path.Combine(directory, file), overwrite: false);
        }
        finally
        {
            File.Delete(temporary);
        }
    }

    static void RequireOwnerOnly(string directory)
    {
        if (!OperatingSystem.IsWindows() && (File.GetUnixFileMode(directory) & ~OwnerDirectory) != UnixFileMode.None)
        {
            throw new DevelopmentProfileRefused("the key directory is accessible to other users; remove it so it can be recreated with owner-only permissions.");
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
        if (!string.Equals(Resolve(expected), expected, StringComparison.Ordinal))
        {
            throw new DevelopmentProfileRefused("the key directory is a symbolic link or is reached through one; the profile never follows links.");
        }

        if (IsUnder(expected, Resolve(contentRoot)))
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

    static List<DevelopmentCertificate> CheckExistingContent(string directory)
    {
        var entries = Directory.EnumerateFileSystemEntries(directory).ToList();
        var marker = Path.Combine(directory, MarkerFile);
        if (entries.Count > 0 && !File.Exists(marker))
        {
            throw new DevelopmentProfileRefused("the key directory already holds material that is not marked as development keys (for example a deployed key ring); it is never adopted.");
        }

        if (File.Exists(marker) && !string.Equals(File.ReadAllText(marker).Trim(), Marker, StringComparison.Ordinal))
        {
            throw new DevelopmentProfileRefused("the development provenance marker is not valid.");
        }

        var certificates = new List<DevelopmentCertificate>();
        foreach (var entry in entries.Where(_ => !Path.GetFileName(_).StartsWith('.')))
        {
            certificates.AddRange(CheckEntry(entry));
        }

        return certificates;
    }

    static bool IsExpected(string name, bool isDirectory) => isDirectory ? name == DataProtectionDirectory : IsDevelopmentFile(name);

    static IEnumerable<DevelopmentCertificate> CheckEntry(string entry)
    {
        var name = Path.GetFileName(entry);
        var isDirectory = Directory.Exists(entry);
        if (new FileInfo(entry).LinkTarget is not null || !IsExpected(name, isDirectory))
        {
            throw new DevelopmentProfileRefused($"the key directory holds an unexpected entry ({name}); only development key files are allowed.");
        }

        if (isDirectory)
        {
            RequireOwnerOnly(entry);
            return [];
        }

        RequireOwnerOnlyFile(entry);
        return name.EndsWith(".crt", StringComparison.Ordinal) ? [ReadCertificate(entry)] : [];
    }

    static void RequireOwnerOnlyFile(string file)
    {
        if (!OperatingSystem.IsWindows() && (File.GetUnixFileMode(file) & ~OwnerFile) != UnixFileMode.None)
        {
            throw new DevelopmentProfileRefused($"the key file {Path.GetFileName(file)} is accessible to other users; remove the key directory so it can be recreated.");
        }
    }

    static DevelopmentCertificate ReadCertificate(string file)
    {
        var name = Path.GetFileName(file);
        try
        {
            using var certificate = X509Certificate2.CreateFromPem(File.ReadAllText(file));
            if (!string.Equals(certificate.SubjectName.Name, new X500DistinguishedName(Subject).Name, StringComparison.Ordinal))
            {
                throw new DevelopmentProfileRefused($"the certificate {name} is not a development certificate.");
            }

            var parts = name.Split('.');
            return new DevelopmentCertificate(parts[0], string.Join('.', parts[1..^1]), new DateTimeOffset(certificate.NotBefore.ToUniversalTime()), new DateTimeOffset(certificate.NotAfter.ToUniversalTime()));
        }
        catch (CryptographicException)
        {
            throw new DevelopmentProfileRefused($"the certificate {name} is malformed.");
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
}

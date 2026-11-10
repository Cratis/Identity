// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Cratis.Identity.Hosting;

namespace Cratis.Identity.Development;

/// <summary>
/// Provisions local-development keys through process-lifetime custody of a validated, pinned directory handle.
/// Only the final leaf may be created: every ancestor must already exist and pass the top-down ownership and access checks.
/// </summary>
/// <remarks>
/// Files are opened without following links, verified on their actual handles and published atomically with owner-only
/// permissions. Runtime key loading and Data Protection retain the same custody instead of reopening this pathname.
/// The profile protects against other local users, not an attacker already running as the current user or administrator.
/// </remarks>
/// <param name="root">The per-user data root, normally the local application data folder.</param>
/// <param name="contentRoot">The content root of the host; the store is refused inside it.</param>
/// <param name="timeProvider">The <see cref="TimeProvider"/>.</param>
/// <param name="beforeDirectoryCreation">Optional synchronization seam before leaf creation.</param>
/// <param name="ancestorOwner">Optional ancestor ownership reader for specifications.</param>
/// <param name="beforeStoreUse">Optional synchronization seam after validation and pinning, before any key material is used.</param>
internal sealed class DevelopmentKeyStore(string root, string contentRoot, TimeProvider timeProvider, Action<string>? beforeDirectoryCreation = null, Func<string, int>? ancestorOwner = null, Action<string>? beforeStoreUse = null)
{
    /// <summary>The development provenance marker file.</summary>
    internal const string MarkerFile = ".cratis-identity-development";

    /// <summary>The content of the provenance marker file.</summary>
    internal const string Marker = "cratis-identity local-development key store. Never copy, deploy or trust.";

    /// <summary>The subject of every development certificate.</summary>
    internal const string Subject = "CN=cratis-identity-local-dev, OU=NOT-FOR-DEPLOYMENT";

    /// <summary>The recognizable prefix of every development key name.</summary>
    internal const string NamePrefix = "dev-";

    /// <summary>The child directory holding the generated Data Protection ring.</summary>
    internal const string DataProtectionDirectory = "data-protection";

    /// <summary>The prefix of an atomic write's temporary file.</summary>
    internal const string TemporaryPrefix = ".tmp-";

    const int KeySize = 3072;
    static readonly TimeSpan _validity = TimeSpan.FromDays(365);
    static readonly TimeSpan _renewBefore = TimeSpan.FromDays(7);
    static readonly string[] _purposes = ["signing", "encryption"];

    /// <summary>Gets the default per-user data root, including when it has not been created yet.</summary>
    public static string DefaultRoot => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify);

    /// <summary>Gets the file system's path comparison.</summary>
    internal static StringComparison PathComparison => OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    /// <summary>
    /// Gets the process-lifetime custody established during provisioning.
    /// </summary>
    internal DevelopmentKeyStoreRoot Custody { get; private set; } = null!;

    /// <summary>Validates and pins the store, then ensures a currently valid signing and encryption pair.</summary>
    /// <param name="authority">The sanitized issuer authority naming the leaf directory.</param>
    /// <returns>The directory, for diagnostics and configuration compatibility only.</returns>
    public string Provision(string authority)
    {
        var directory = ResolveLocation(root, contentRoot, authority);
        Custody = DevelopmentKeyStoreRoot.Open(directory, beforeDirectoryCreation, ancestorOwner);
        try
        {
            beforeStoreUse?.Invoke(directory);
            Provision(Custody);
            return directory;
        }
        catch
        {
            Custody.Dispose();
            throw;
        }
    }

    /// <summary>Gets whether a path is the parent or lies below it, using the file system's case rules.</summary>
    /// <param name="path">The path to test.</param>
    /// <param name="parent">The parent.</param>
    /// <returns>Whether the path equals or is inside the parent.</returns>
    internal static bool IsUnderOrEqual(string path, string parent)
    {
        var normalizedPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
        var normalizedParent = Path.TrimEndingDirectorySeparator(Path.GetFullPath(parent));
        return string.Equals(normalizedPath, normalizedParent, PathComparison) || normalizedPath.StartsWith(normalizedParent + Path.DirectorySeparatorChar, PathComparison);
    }

    /// <summary>
    /// Determines whether a name matches the development certificate or private-key naming convention.
    /// </summary>
    /// <param name="name">The child file name.</param>
    /// <returns>Whether the name is a recognized development key file.</returns>
    internal static bool IsDevelopmentFile(string name)
    {
        var parts = name.Split('.');
        return parts.Length == 3 && _purposes.Contains(parts[0], StringComparer.Ordinal) &&
            parts[1].StartsWith(NamePrefix, StringComparison.Ordinal) && parts[1].All(_ => char.IsAsciiLetterOrDigit(_) || _ == '-') &&
            (parts[2] == "crt" || parts[2] == "key");
    }

    /// <summary>
    /// Validates the configured location before any host bootstrap creates parent folders.
    /// </summary>
    /// <param name="authority">The sanitized issuer authority naming the leaf directory.</param>
    internal void ValidateLocation(string authority) => _ = ResolveLocation(root, contentRoot, authority);

    static string ResolveLocation(string dataRoot, string applicationRoot, string authority)
    {
        if (string.IsNullOrWhiteSpace(dataRoot))
        {
            throw new DevelopmentProfileRefused("the per-user application data folder is not available on this machine.");
        }

        var expected = Path.Combine(Resolve(dataRoot), "cratis", "identity-dev", authority);
        if (IsUnderOrEqual(expected, Resolve(applicationRoot)))
        {
            throw new DevelopmentProfileRefused("the key directory is inside the application content root, where it could be committed.");
        }

        return expected;
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

    static ExistingContent CheckExistingContent(DevelopmentKeyStoreRoot directory)
    {
        var entries = directory.Entries();
        var hasMarker = entries.Contains(MarkerFile, StringComparer.Ordinal);
        if (entries.Count > 0 && !hasMarker)
        {
            throw new DevelopmentProfileRefused("the key directory already holds material that is not marked as development keys (for example a deployed key ring); it is never adopted.");
        }

        if (hasMarker)
        {
            directory.RequireMarker();
        }

        var content = new ExistingContent { HasMarker = hasMarker };
        foreach (var name in entries.Where(_ => _ != MarkerFile))
        {
            CheckEntry(directory, name, content);
        }

        return content;
    }

    static void CheckEntry(DevelopmentKeyStoreRoot directory, string name, ExistingContent content)
    {
        if (name.StartsWith(TemporaryPrefix, StringComparison.Ordinal))
        {
            throw new DevelopmentProfileRefused($"the key directory holds a partial write ({name}) left by an earlier run; remove the key directory so it can be recreated.");
        }

        if (name == DataProtectionDirectory)
        {
            _ = directory.GetDirectory(name);
            return;
        }

        if (!IsDevelopmentFile(name))
        {
            throw new DevelopmentProfileRefused($"the key directory holds an unexpected entry ({name}); only development key files are allowed.");
        }

        using var stream = directory.OpenRead(name);
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

    static void RemoveOrphanKeys(DevelopmentKeyStoreRoot directory, ExistingContent content)
    {
        var certificates = content.Certificates.Select(_ => $"{_.Purpose}.{_.Name}.key").ToHashSet(StringComparer.Ordinal);
        var missing = certificates.FirstOrDefault(_ => !content.Keys.Contains(_));
        if (missing is not null)
        {
            throw new DevelopmentProfileRefused($"the certificate for {missing} has no private key; remove the key directory so it can be recreated.");
        }

        foreach (var key in content.Keys.Where(_ => !certificates.Contains(_)))
        {
            directory.Delete(key);
        }
    }

    static void Create(DevelopmentKeyStoreRoot directory, string purpose, string name, DateTimeOffset now)
    {
        using var rsa = RSA.Create(KeySize);
        var request = new CertificateRequest(Subject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(now.AddMinutes(-1), now.Add(_validity));
        directory.WriteAtomically($"{purpose}.{name}.key", rsa.ExportPkcs8PrivateKeyPem());
        directory.WriteAtomically($"{purpose}.{name}.crt", certificate.ExportCertificatePem());
    }

    void Provision(DevelopmentKeyStoreRoot directory)
    {
        directory.VerifyIdentity();
        var content = CheckExistingContent(directory);
        RemoveOrphanKeys(directory, content);
        if (!content.HasMarker)
        {
            directory.WriteAtomically(MarkerFile, Marker);
        }

        _ = directory.GetDirectory(DataProtectionDirectory);
        var now = timeProvider.GetUtcNow();
        var valid = content.Certificates.Where(_ => _.NotBefore <= now && now < _.NotAfter - _renewBefore).Select(_ => _.Purpose).ToHashSet();
        var taken = content.Certificates.Select(_ => _.Name).Concat(content.KeyNames).ToHashSet(StringComparer.Ordinal);
        foreach (var purpose in _purposes.Where(_ => !valid.Contains(_)))
        {
            Create(directory, purpose, NewName(taken, now), now);
        }
    }

    sealed record DevelopmentCertificate(string Purpose, string Name, DateTimeOffset NotBefore, DateTimeOffset NotAfter);

    sealed class ExistingContent
    {
        public bool HasMarker { get; init; }

        public List<DevelopmentCertificate> Certificates { get; } = [];

        public HashSet<string> Keys { get; } = new(StringComparer.Ordinal);

        public HashSet<string> KeyNames { get; } = new(StringComparer.Ordinal);
    }
}

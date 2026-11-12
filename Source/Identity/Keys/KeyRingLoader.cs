// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using System.Xml.Linq;
using Microsoft.IdentityModel.Tokens;

namespace Cratis.Identity.Keys;

/// <summary>
/// Builds a <see cref="KeyRing"/> from the files of the secret store, and refuses the whole snapshot when any file is not valid.
/// </summary>
/// <remarks>
/// Layout: <c language="text">signing.&lt;name&gt;.crt</c> + <c language="text">.key</c>,
/// <c language="text">encryption.&lt;name&gt;.crt</c> + <c language="text">.key</c> and
/// <c language="text">data-protection.&lt;name&gt;.xml</c>. Certificates are PEM, keys are unencrypted PKCS#8 PEM.
/// Signing keys are RSA of at least 2048 bits or ECDSA P-256, encryption keys are RSA of at least 2048 bits.
/// </remarks>
internal static class KeyRingLoader
{
    const string Signing = "signing";
    const string Encryption = "encryption";
    const string DataProtection = "data-protection";
    const int MinimumRsaKeySize = 2048;
    const string P256Oid = "1.2.840.10045.3.1.7";

    delegate void ImportPem(ReadOnlySpan<char> pem);

    /// <summary>
    /// Builds the ring.
    /// </summary>
    /// <param name="files">The file contents by file name.</param>
    /// <returns>The validated <see cref="KeyRing"/>.</returns>
    /// <exception cref="InvalidKeyMaterial">Thrown when any file is not valid.</exception>
    public static KeyRing Load(IReadOnlyDictionary<string, string> files)
    {
        var entries = files.OrderBy(_ => _.Key, StringComparer.Ordinal).Select(_ => SecretFile.Parse(_.Key, _.Value)).ToList();

        var keys = entries
            .Where(_ => _.Purpose != DataProtection)
            .GroupBy(_ => (_.Purpose, _.Name))
            .Select(_ => LoadPair(_.Key.Purpose == Signing ? KeyPurpose.Signing : KeyPurpose.Encryption, _.Key.Name, _.ToList()))
            .ToList();

        RefuseDuplicates(keys);

        return new KeyRing(
            keys.Where(_ => _.Purpose == KeyPurpose.Signing).ToList(),
            keys.Where(_ => _.Purpose == KeyPurpose.Encryption).ToList(),
            entries.Where(_ => _.Purpose == DataProtection).Select(LoadDataProtection).ToList());
    }

    static RealmKey LoadPair(KeyPurpose purpose, string name, List<SecretFile> pair)
    {
        var certificateFile = pair.FirstOrDefault(_ => _.Extension == "crt");
        var keyFile = pair.FirstOrDefault(_ => _.Extension == "key");
        if (certificateFile is null || keyFile is null)
        {
            var present = (certificateFile ?? keyFile)!;
            throw new InvalidKeyMaterial(present.File, certificateFile is null ? "there is no matching .crt file" : "there is no matching .key file");
        }

        if (keyFile.Content.Contains("ENCRYPTED", StringComparison.Ordinal))
        {
            throw new InvalidKeyMaterial(keyFile.File, "the private key is encrypted; only unencrypted PKCS#8 PEM is supported");
        }

        X509Certificate2 certificate;
        try
        {
            certificate = X509Certificate2.CreateFromPem(certificateFile.Content);
        }
        catch (CryptographicException)
        {
            throw new InvalidKeyMaterial(certificateFile.File, "the certificate PEM is malformed");
        }

        using (certificate)
        {
            var (withKey, algorithm) = AttachKey(purpose, certificate, certificateFile, keyFile);
            return new RealmKey(name, withKey.Thumbprint, withKey, purpose, algorithm);
        }
    }

    static (X509Certificate2 Certificate, string Algorithm) AttachKey(KeyPurpose purpose, X509Certificate2 certificate, SecretFile certificateFile, SecretFile keyFile)
    {
        var signing = purpose == KeyPurpose.Signing;
        if (certificate.GetRSAPublicKey() is not null)
        {
            // The certificate copy keeps using the key, so it is not disposed here.
#pragma warning disable CA2000
            var rsa = RSA.Create();
#pragma warning restore CA2000
            Import(keyFile, rsa.ImportFromPem);
            Require(SamePublicKey(certificate, rsa.ExportSubjectPublicKeyInfo()), keyFile, "the private key does not belong to the certificate");
            Require(rsa.KeySize >= MinimumRsaKeySize, keyFile, "the RSA key is shorter than 2048 bits");
            return (certificate.CopyWithPrivateKey(rsa), signing ? SecurityAlgorithms.RsaSha256 : SecurityAlgorithms.RsaOAEP);
        }

        Require(signing, certificateFile, "encryption keys must be RSA");
        Require(certificate.GetECDsaPublicKey() is not null, certificateFile, "the key algorithm is not supported; use RSA or ECDSA P-256");

#pragma warning disable CA2000
        var ecdsa = ECDsa.Create();
#pragma warning restore CA2000
        Import(keyFile, ecdsa.ImportFromPem);
        Require(SamePublicKey(certificate, ecdsa.ExportSubjectPublicKeyInfo()), keyFile, "the private key does not belong to the certificate");
        var curve = ecdsa.ExportParameters(false).Curve.Oid;
        Require(curve?.Value == P256Oid || curve?.FriendlyName == "nistP256", keyFile, "the ECDSA key is not on curve P-256");
        return (certificate.CopyWithPrivateKey(ecdsa), SecurityAlgorithms.EcdsaSha256);
    }

    static void Import(SecretFile file, ImportPem import)
    {
        try
        {
            import(file.Content);
        }
        catch (Exception exception) when (exception is CryptographicException or ArgumentException)
        {
            throw new InvalidKeyMaterial(file.File, "the private key PEM is malformed or is not a PKCS#8 key");
        }
    }

    static void Require(bool condition, SecretFile file, string reason)
    {
        if (!condition)
        {
            throw new InvalidKeyMaterial(file.File, reason);
        }
    }

    static bool SamePublicKey(X509Certificate2 certificate, byte[] publicKey) =>
        certificate.PublicKey.ExportSubjectPublicKeyInfo().AsSpan().SequenceEqual(publicKey);

    static void RefuseDuplicates(List<RealmKey> keys)
    {
        var seen = new Dictionary<string, RealmKey>(StringComparer.Ordinal);
        foreach (var key in keys)
        {
            var material = Convert.ToHexString(SHA256.HashData(key.Certificate.PublicKey.ExportSubjectPublicKeyInfo()));
            if (!seen.TryGetValue(material, out var first))
            {
                seen[material] = key;
                continue;
            }

            var reason = (first.Purpose != key.Purpose, first.Kid == key.Kid) switch
            {
                (true, _) => "the same key material serves both signing and encryption",
                (_, true) => "the same certificate is present twice",
                _ => "the same key material is present twice"
            };
            throw new InvalidKeyMaterial(FileName(key), reason);
        }
    }

    static string FileName(RealmKey key) => $"{key.Purpose.ToString().ToLowerInvariant()}.{key.Name}.crt";

    static DataProtectionKey LoadDataProtection(SecretFile file)
    {
        XElement element;
        try
        {
            using var reader = XmlReader.Create(new StringReader(file.Content), new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
            element = XElement.Load(reader);
        }
        catch (XmlException)
        {
            throw new InvalidKeyMaterial(file.File, "the XML is malformed");
        }

        Require(
            element.Name.LocalName == "key" && element.Element("descriptor") is not null && Guid.TryParse(element.Attribute("id")?.Value, out _),
            file,
            "it is not a Data Protection key element");
        _ = ReadDate(element, "creationDate", file);

        return new DataProtectionKey(file.Name, element, ReadDate(element, "activationDate", file), ReadDate(element, "expirationDate", file));
    }

    static DateTimeOffset ReadDate(XElement element, string name, SecretFile file)
    {
        Require(
            DateTimeOffset.TryParse(element.Element(name)?.Value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date),
            file,
            $"the {name} is missing or not a date");
        return date;
    }

    sealed record SecretFile(string File, string Content)
    {
        public string Purpose { get; private init; } = string.Empty;

        public string Name { get; private init; } = string.Empty;

        public string Extension { get; private init; } = string.Empty;

        public static SecretFile Parse(string file, string content)
        {
            var parts = file.Split('.');
            var recognized = parts.Length >= 3 &&
                parts[1..^1].All(_ => _.Length > 0) &&
                (parts[0], parts[^1]) is (Signing or Encryption, "crt" or "key") or (DataProtection, "xml");
            return recognized
                ? new SecretFile(file, content) { Purpose = parts[0], Name = string.Join('.', parts[1..^1]), Extension = parts[^1] }
                : throw new InvalidKeyMaterial(file, "the file name is not signing.<name>.crt|key, encryption.<name>.crt|key or data-protection.<name>.xml");
        }
    }
}

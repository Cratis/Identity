// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Cratis.Identity.Integration.given;

/// <summary>
/// A temporary directory laid out like a mounted secret store, with throwaway keys generated per spec run.
/// </summary>
public sealed class SecretStoreDirectory : IDisposable
{
    public SecretStoreDirectory() => Directory.CreateDirectory(Path);

    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"identity-secrets-{Guid.NewGuid():N}");

    /// <summary>
    /// Writes an RSA signing or encryption pair.
    /// </summary>
    /// <param name="purpose"><c language="text">signing</c> or <c language="text">encryption</c>.</param>
    /// <param name="name">The key name.</param>
    /// <param name="notBefore">When the certificate becomes valid.</param>
    /// <returns>The kid, which is the certificate thumbprint.</returns>
    public string WriteKey(string purpose, string name, DateTimeOffset notBefore)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=spec", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(notBefore, notBefore.AddDays(30));
        File.WriteAllText(System.IO.Path.Combine(Path, $"{purpose}.{name}.crt"), certificate.ExportCertificatePem());
        File.WriteAllText(System.IO.Path.Combine(Path, $"{purpose}.{name}.key"), rsa.ExportPkcs8PrivateKeyPem());
        return certificate.Thumbprint;
    }

    /// <summary>
    /// Writes a Data Protection key valid from yesterday for 90 days.
    /// </summary>
    public void WriteDataProtectionKey()
    {
        var now = DateTimeOffset.UtcNow;
        var masterKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var element = string.Join(
            Environment.NewLine,
            $"<key id=\"{Guid.NewGuid()}\" version=\"1\">",
            $"<creationDate>{now.AddDays(-1):O}</creationDate>",
            $"<activationDate>{now.AddDays(-1):O}</activationDate>",
            $"<expirationDate>{now.AddDays(90):O}</expirationDate>",
            "<descriptor deserializerType=\"Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.AuthenticatedEncryptorDescriptorDeserializer, Microsoft.AspNetCore.DataProtection, Version=10.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60\">",
            "<descriptor>",
            "<encryption algorithm=\"AES_256_CBC\" />",
            "<validation algorithm=\"HMACSHA256\" />",
            "<masterKey enc:requiresEncryption=\"true\" xmlns:enc=\"http://schemas.asp.net/2015/03/dataProtection\">",
            $"<value>{masterKey}</value>",
            "</masterKey>",
            "</descriptor>",
            "</descriptor>",
            "</key>");
        File.WriteAllText(System.IO.Path.Combine(Path, "data-protection.a.xml"), element);
    }

    /// <summary>
    /// Removes a key pair, as an operator retiring it.
    /// </summary>
    /// <param name="purpose"><c language="text">signing</c> or <c language="text">encryption</c>.</param>
    /// <param name="name">The key name.</param>
    public void RemoveKey(string purpose, string name)
    {
        File.Delete(System.IO.Path.Combine(Path, $"{purpose}.{name}.crt"));
        File.Delete(System.IO.Path.Combine(Path, $"{purpose}.{name}.key"));
    }

    /// <inheritdoc/>
    public void Dispose() => Directory.Delete(Path, true);
}

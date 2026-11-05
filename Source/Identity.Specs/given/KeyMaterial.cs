// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Cratis.Identity.given;

/// <summary>
/// Generates throwaway key material for specs. Nothing is ever committed as a file.
/// </summary>
public static class KeyMaterial
{
    public static readonly DateTimeOffset Now = new(2030, 6, 1, 12, 0, 0, TimeSpan.Zero);

    public static PemPair Rsa(DateTimeOffset notBefore, DateTimeOffset notAfter, int bits = 2048)
    {
        using var rsa = RSA.Create(bits);
        var request = new CertificateRequest("CN=spec", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(notBefore, notAfter);
        return new PemPair(certificate.ExportCertificatePem(), rsa.ExportPkcs8PrivateKeyPem());
    }

    public static PemPair Ec(DateTimeOffset notBefore, DateTimeOffset notAfter, ECCurve? curve = null)
    {
        using var ecdsa = ECDsa.Create(curve ?? ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest("CN=spec", ecdsa, HashAlgorithmName.SHA256);
        using var certificate = request.CreateSelfSigned(notBefore, notAfter);
        return new PemPair(certificate.ExportCertificatePem(), ecdsa.ExportPkcs8PrivateKeyPem());
    }

    public static PemPair CurrentRsa(int bits = 2048) => Rsa(Now - TimeSpan.FromHours(2), Now + TimeSpan.FromDays(365), bits);

    public static string EncryptedKey(PemPair pair)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(pair.Key);
        return rsa.ExportEncryptedPkcs8PrivateKeyPem("password", new PbeParameters(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, 1000));
    }

    public static string KeyThumbprint(PemPair pair)
    {
        using var certificate = X509Certificate2.CreateFromPem(pair.Certificate);
        return certificate.Thumbprint;
    }

    public static string DataProtectionKey(DateTimeOffset activation, DateTimeOffset expiration)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Keys", "data-protection-key.template.xml");
        return File.ReadAllText(path)
            .Replace("{{ID}}", Guid.NewGuid().ToString(), StringComparison.Ordinal)
            .Replace("{{CREATION}}", activation.ToString("O"), StringComparison.Ordinal)
            .Replace("{{ACTIVATION}}", activation.ToString("O"), StringComparison.Ordinal)
            .Replace("{{EXPIRATION}}", expiration.ToString("O"), StringComparison.Ordinal)
            .Replace("{{MASTER_KEY}}", Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)), StringComparison.Ordinal);
    }
}

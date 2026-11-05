// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.given;

/// <summary>
/// An <see cref="IdentityHost"/> whose keys come from a secret store directory that specs change while it runs.
/// </summary>
public class RotatingIdentityHost : IdentityHost
{
    public RotatingIdentityHost()
    {
        Secrets = new SecretStoreDirectory();
        InitialSigningKid = Secrets.WriteKey("signing", "a", DateTimeOffset.UtcNow.AddHours(-2));
        Secrets.WriteKey("encryption", "a", DateTimeOffset.UtcNow.AddHours(-2));
        Secrets.WriteDataProtectionKey();
    }

    public SecretStoreDirectory Secrets { get; }

    public string InitialSigningKid { get; }

    /// <inheritdoc/>
    protected override Dictionary<string, string?> Settings()
    {
        var settings = base.Settings();
        settings["Identity:SecretStore:Path"] = Secrets.Path;
        settings["Identity:SecretStore:ReloadInterval"] = "00:00:01";
        return settings;
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            Secrets.Dispose();
        }
    }
}

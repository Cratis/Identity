// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.given;

/// <summary>A separate, opt-in host; the existing integration fixture remains Bearer-only.</summary>
public class DPoPHost : IdentityHost
{
    /// <inheritdoc/>
    protected override Dictionary<string, string?> Settings()
    {
        var settings = base.Settings();
        settings["Identity:DPoP:Enabled"] = "true";
        settings["Identity:DPoP:PublicTokenEndpoint"] = Issuer + "connect/token";
        return settings;
    }
}

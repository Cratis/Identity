// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.given;

/// <summary>
/// An Identity host that opts into OpenID Connect with the three test web clients and a recorded consent decision.
/// </summary>
public class OidcIdentityHost : IdentityHost
{
    /// <inheritdoc/>
    protected override Dictionary<string, string?> Settings()
    {
        var settings = base.Settings();
        settings["Identity:Oidc:IssuanceEnabled"] = "true";
        settings["Identity:Oidc:ConsentDecision"] = "ImplicitFencedPath";
        SpecClientDescriptors.AddOidcClients(settings);
        settings["Identity:OidcClients:0:Scopes:2"] = "offline_access";
        return settings;
    }
}

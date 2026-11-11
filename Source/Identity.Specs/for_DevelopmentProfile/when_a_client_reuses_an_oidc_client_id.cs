// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Hosting;

namespace Cratis.Identity.for_DevelopmentProfile;

public class when_a_client_reuses_an_oidc_client_id : given.a_development_profile
{
    void Establish()
    {
        _settings["Identity:OidcClients:0:ClientId"] = "rp-web";
        _settings["Identity:OidcClients:0:ClientSecret"] = "test-only-secret";
        _settings["Identity:OidcClients:0:RedirectUris:0"] = "https://rp.example/signin-oidc";
        _settings["Identity:Development:Clients:0:ClientId"] = "rp-web";
    }

    void Because() => Apply();

    [Fact] void should_refuse_it() => _exception.ShouldBeOfExactType<DevelopmentProfileRefused>();
}

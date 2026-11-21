// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;
using Cratis.Identity.Configuration;
using Cratis.Identity.Hosting;

namespace Cratis.Identity.for_OidcProviderMetadata;

public class when_issuance_is_enabled : Specification
{
    Dictionary<string, object> _metadata;

    void Because()
    {
        var options = new IdentityOptions { Issuer = "https://identity.example/" };
        options.Oidc.IssuanceEnabled = true;
        _metadata = OidcProviderMetadata.Create(options, new IdentityEndpointOptions { RoutePrefix = "/identity" });
    }

    [Fact] void should_advertise_only_rs256() => ((string[])_metadata["id_token_signing_alg_values_supported"]).ShouldContainOnly("RS256");
    [Fact] void should_preserve_the_route_prefix() => _metadata["userinfo_endpoint"].ShouldEqual("https://identity.example/identity/connect/userinfo");
    [Fact] void should_not_advertise_an_unregistered_logout_endpoint() => _metadata.ContainsKey("end_session_endpoint").ShouldBeFalse();
    [Fact] void should_advertise_the_expiration_claim() => ((string[])_metadata["claims_supported"]).ShouldContain("exp");
    [Fact] void should_preserve_the_authentication_time_claim() => ((string[])_metadata["claims_supported"]).ShouldContain("auth_time");
    [Fact] void should_advertise_max_age_handling() => _metadata["max_age_supported"].ShouldEqual(true);
    [Fact] void should_be_code_only() => ((string[])_metadata["response_types_supported"]).ShouldContainOnly("code");
    [Fact] void should_not_advertise_undefined_assurance_or_email() => ((string[])_metadata["claims_supported"]).ShouldNotContain("email");
}

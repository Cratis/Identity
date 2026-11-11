// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;
using Cratis.Identity.Configuration;
using Cratis.Identity.Hosting;

namespace Cratis.Identity.for_OidcProviderMetadata;

public class when_issuance_is_disabled : Specification
{
    Dictionary<string, object> _metadata;

    void Because() => _metadata = OidcProviderMetadata.Create(new IdentityOptions { Issuer = "https://identity.example/" }, new IdentityEndpointOptions());

    [Fact] void should_publish_only_issuer_and_keys() => _metadata.Keys.ShouldContainOnly("issuer", "jwks_uri");
    [Fact] void should_use_the_configured_public_issuer() => _metadata["issuer"].ShouldEqual("https://identity.example/");
    [Fact] void should_not_depend_on_request_headers() => _metadata["jwks_uri"].ShouldEqual("https://identity.example/.well-known/jwks");
}

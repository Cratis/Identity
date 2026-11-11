// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;
using Cratis.Identity.Configuration;
using Cratis.Identity.Hosting;

namespace Cratis.Identity.for_OidcProviderMetadata;

public class when_the_end_session_endpoint_is_registered : Specification
{
    Dictionary<string, object> _metadata;

    void Because()
    {
        var options = new IdentityOptions { Issuer = "https://identity.example/" };
        options.Oidc.IssuanceEnabled = true;
        _metadata = OidcProviderMetadata.Create(options, new IdentityEndpointOptions { RoutePrefix = "/identity" }, endSessionRegistered: true);
    }

    [Fact] void should_advertise_the_prefixed_registered_endpoint() => _metadata["end_session_endpoint"].ShouldEqual("https://identity.example/identity/connect/endsession");
}

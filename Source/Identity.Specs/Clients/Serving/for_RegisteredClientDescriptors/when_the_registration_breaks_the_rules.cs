// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Cratis.Identity.Contracts.Clients;

namespace Cratis.Identity.Clients.Serving.for_RegisteredClientDescriptors;

public class when_the_registration_breaks_the_rules : given.client_commands
{
    Exception _publicWithoutPkce;
    Exception _nativeWithRemoteRedirect;

    void Because()
    {
        _publicWithoutPkce = Catch.Exception(() => RegisteredClientDescriptors.For(_active with
        {
            Configuration = _configuration with { ClientType = ClientType.Public, RequirePkce = false }
        }));
        _nativeWithRemoteRedirect = Catch.Exception(() => RegisteredClientDescriptors.For(_active with
        {
            Configuration = _configuration with
            {
                ClientType = ClientType.Public,
                ApplicationType = ClientApplicationType.Native,
                RedirectUris = ["https://client.example/callback"]
            }
        }));
    }

    [Fact] void should_refuse_a_public_client_without_pkce() => _publicWithoutPkce.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_refuse_a_native_client_with_a_remote_redirect() => _nativeWithRemoteRedirect.ShouldBeOfExactType<InvalidOperationException>();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Clients;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.for_ClientRegistrationRules;

public class when_validating_client_proof : Specification
{
    [Fact]
    public void should_refuse_s256_when_plain_is_also_enabled()
    {
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientType = ClientTypes.Public,
            Requirements = { Requirements.Features.ProofKeyForCodeExchange }
        };
        ClientRegistrationRules.Validate(descriptor, [CodeChallengeMethods.Sha256, CodeChallengeMethods.Plain]).IsValid.ShouldBeFalse();
    }

    [Theory]
    [InlineData(ClientTypes.Public, true, CodeChallengeMethods.Sha256, null, true)]
    [InlineData(ClientTypes.Public, false, CodeChallengeMethods.Sha256, null, false)]
    [InlineData(ClientTypes.Public, true, CodeChallengeMethods.Plain, null, false)]
    [InlineData(ClientTypes.Public, true, null, null, false)]
    [InlineData(ClientTypes.Confidential, false, null, null, false)]
    [InlineData(ClientTypes.Confidential, false, null, " ", false)]
    [InlineData(ClientTypes.Confidential, false, null, "synthetic-registration-secret", true)]
    public void should_require_the_client_proof(string clientType, bool pkce, string? method, string? secret, bool accepted)
    {
        var descriptor = new OpenIddictApplicationDescriptor { ClientType = clientType, ClientSecret = secret };
        if (pkce)
        {
            descriptor.Requirements.Add(Requirements.Features.ProofKeyForCodeExchange);
        }

        var result = ClientRegistrationRules.Validate(descriptor, method is null ? [] : [method]);
        result.IsValid.ShouldEqual(accepted);
        (result.Refusals.Count == 0).ShouldEqual(accepted);
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Clients;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.for_ClientRegistrationRules;

public class when_validating_development_redirects : Specification
{
    [Theory]
    [InlineData("http://localhost/callback", true, true)]
    [InlineData("http://localhost/callback", false, false)]
    [InlineData("http://127.0.0.1/callback", true, true)]
    [InlineData("https://localhost/callback", true, false)]
    [InlineData("http://localhost.example.com/callback", true, false)]
    [InlineData("http://example.com/callback", true, false)]
    public void should_allow_localhost_only_when_asked(string redirect, bool allowLocalhost, bool accepted)
    {
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientType = ClientTypes.Public,
            ApplicationType = ApplicationTypes.Native,
            RedirectUris = { new Uri(redirect) },
            Requirements = { Requirements.Features.ProofKeyForCodeExchange }
        };
        ClientRegistrationRules.Validate(descriptor, [CodeChallengeMethods.Sha256], allowLocalhost).IsValid.ShouldEqual(accepted);
    }
}

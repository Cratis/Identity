// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Clients;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.for_ClientRegistrationRules;

public class when_validating_native_redirects : Specification
{
    [Theory]
    [InlineData("http://127.0.0.1:49152/callback", true)]
    [InlineData("http://[::1]:49152/callback", true)]
    [InlineData("http://localhost/callback", false)]
    [InlineData("http://example.com/callback", false)]
    [InlineData("https://127.0.0.1/callback", false)]
    [InlineData("http://127.0.0.2/callback", false)]
    [InlineData("http://127.0.0.1/callback#fragment", false)]
    [InlineData("http://user@127.0.0.1/callback", false)]
    [InlineData("/callback", false)]
    public void should_apply_the_redirect_rules(string redirect, bool accepted)
    {
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientType = ClientTypes.Public,
            ApplicationType = ApplicationTypes.Native,
            RedirectUris = { new Uri(redirect, UriKind.RelativeOrAbsolute) },
            Requirements = { Requirements.Features.ProofKeyForCodeExchange }
        };
        ClientRegistrationRules.Validate(descriptor, [CodeChallengeMethods.Sha256]).IsValid.ShouldEqual(accepted);
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_IdentityOptionsValidation;

public class when_validating_oidc_scopes : given.an_oidc_configuration
{
    [Theory]
    [InlineData("openid", true)]
    [InlineData("openid profile", true)]
    [InlineData("openid offline_access", true)]
    [InlineData("openid profile offline_access", true)]
    [InlineData("profile", false)]
    [InlineData("offline_access", false)]
    [InlineData("openid email", false)]
    [InlineData("openid direct:read", false)]
    [InlineData("OpenId", false)]
    [InlineData("", false)]
    void should_require_openid_and_only_supported_identity_scopes(string scopes, bool valid)
    {
        client.Scopes = scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        Validate().Succeeded.ShouldEqual(valid);
    }
}

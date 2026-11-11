// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.given;
using Cratis.Identity.Keys;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.for_OidcSigningKey;

public class when_only_an_ec_signer_is_available : for_SecretStoreKeys.given.a_secret_store_directory
{
    Exception _failure;

    void Establish()
    {
        new StoreFiles().Pair("signing", "a", KeyMaterial.Ec(KeyMaterial.Now.AddHours(-2), KeyMaterial.Now.AddDays(90))).WriteTo(_directory);
        Open();
    }

    void Because() => _failure = Catch.Exception(() => OidcSigningKey.Require(_keys, _time.GetUtcNow()));

    [Fact] void should_refuse_oidc_startup() => _failure.ShouldBeOfExactType<OptionsValidationException>();
    [Fact] void should_explain_the_required_algorithm() => _failure.Message.ShouldContain("RS256");
    [Fact] void should_leave_oauth_key_eligibility_unchanged() => _keys.CanIssue.ShouldBeTrue();
}

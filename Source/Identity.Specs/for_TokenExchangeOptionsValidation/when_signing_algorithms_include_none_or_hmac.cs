// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Cratis.Identity.given;

namespace Cratis.Identity.for_TokenExchangeOptionsValidation;

public class when_signing_algorithms_include_none_or_hmac : Specification
{
    string[] _refused = ["none", "HS256", "HS512", "", "rs256"];
    bool[] _failed;

    void Because() => _failed = [.. _refused.Select(algorithm =>
    {
        var options = Configurations.WithTokenExchange();
        options.TokenExchange.Issuers[0].SigningAlgorithms = [algorithm];
        return new IdentityOptionsValidation(new Microsoft.Extensions.Hosting.Internal.HostingEnvironment { EnvironmentName = "Production" }).Validate(null, options).Failed;
    })];

    [Fact] void should_refuse_every_one() => _failed.All(_ => _).ShouldBeTrue();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;

namespace Cratis.Identity.for_IdentityOptionsValidation;

public class when_validating_registration_configuration : Specification
{
    [Theory]
    [InlineData("multiple-indicators")]
    [InlineData("duplicate-audience")]
    [InlineData("duplicate-indicator")]
    [InlineData("normalized-duplicate-indicator")]
    [InlineData("missing-indicator")]
    [InlineData("empty-owner")]
    [InlineData("blank-owner")]
    public void should_refuse_invalid_registration_configuration(string configuration)
    {
        var options = new IdentityOptions
        {
            Resources =
            [
                new() { Audience = "first-api", Indicators = ["https://first.example.test/"], Scopes = [new() { Name = "first:read" }] },
                new() { Audience = "second-api", Indicators = ["https://second.example.test/"], Scopes = [new() { Name = "second:read" }] }
            ]
        };
        switch (configuration)
        {
            case "multiple-indicators": options.Resources[0].Indicators.Add("https://first.example.test/other"); break;
            case "duplicate-audience": options.Resources[1].Audience = options.Resources[0].Audience; break;
            case "duplicate-indicator": options.Resources[1].Indicators = options.Resources[0].Indicators; break;
            case "normalized-duplicate-indicator": options.Resources[1].Indicators = ["https://FIRST.example.test:443/"]; break;
            case "missing-indicator": options.Resources[0].Indicators.Clear(); break;
            case "empty-owner": options.SecurityOwners.Add(string.Empty); break;
            case "blank-owner": options.SecurityOwners.Add(" "); break;
        }

        new IdentityOptionsValidation(new HostingEnvironment { EnvironmentName = Environments.Development }).Validate(null, options).Failed.ShouldBeTrue();
    }
}

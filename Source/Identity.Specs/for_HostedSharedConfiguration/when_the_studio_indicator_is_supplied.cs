// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_HostedSharedConfiguration;

public class when_the_studio_indicator_is_supplied : given.a_hosted_studio_configuration
{
    protected override string? StudioIndicator => "https://studio.example/mcp";

    Microsoft.Extensions.Options.ValidateOptionsResult _result;

    void Because() => _result = Validate();

    [Fact] void should_accept_an_https_indicator_callback_and_long_secret() => _result.Succeeded.ShouldBeTrue();
}

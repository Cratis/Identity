// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_HostedSharedConfiguration;

public class when_a_hosted_indicator_is_not_https : given.a_hosted_studio_configuration
{
    protected override string? StudioIndicator => "http://studio.example/mcp";

    Microsoft.Extensions.Options.ValidateOptionsResult _result;

    void Because() => _result = Validate();

    [Fact] void should_refuse_the_configuration() => _result.Failed.ShouldBeTrue();
    [Fact] void should_name_the_audience() => string.Join('\n', _result.Failures).ShouldContain("studio-mcp");
    [Fact] void should_not_quote_the_indicator() => string.Join('\n', _result.Failures).ShouldNotContain("http://studio.example");
}

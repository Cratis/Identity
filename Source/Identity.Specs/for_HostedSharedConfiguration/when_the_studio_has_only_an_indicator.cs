// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_HostedSharedConfiguration;

public class when_the_studio_has_only_an_indicator : given.a_hosted_studio_configuration
{
    protected override string? StudioIndicator => "https://studio.example/mcp";

    protected override string? StudioUrl => null;

    protected override string? StudioSecret => null;

    Microsoft.Extensions.Options.ValidateOptionsResult _result;

    void Because() => _result = Validate();

    [Fact] void should_refuse_the_configuration() => _result.Failed.ShouldBeTrue();
    [Fact] void should_name_the_audience() => string.Join('\n', _result.Failures).ShouldContain("studio-mcp");
}

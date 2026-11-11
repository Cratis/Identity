// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.for_TokenRateLimitMiddleware;

public class when_starting_with_invalid_rate_limit_options : given.an_identity_host
{
    [Theory]
    [InlineData("Window", "00:00:00")]
    [InlineData("Window", "02:00:00")]
    [InlineData("PerSourceRequests", "0")]
    [InlineData("PerClientRequests", "0")]
    [InlineData("PerClientExchangeRequests", "0")]
    [InlineData("MaxTrackedKeys", "99")]
    [InlineData("MaxTrackedKeys", "1000001")]
    public async Task should_fail_options_validation_at_host_startup(string setting, string value)
    {
        Builder.Configuration[$"Identity:TokenRateLimit:{setting}"] = value;
        RegisterIdentity();
        App = Builder.Build();

        // Mapping endpoints also resolves IOptions: isolate ValidateOnStart so the host itself must reject these options.
        var exception = await Assert.ThrowsAsync<OptionsValidationException>(() => App.StartAsync(TestContext.Current.CancellationToken));

        exception.OptionsType.ShouldEqual(typeof(IdentityOptions));
        exception.Failures.Any(failure => failure.StartsWith($"TokenRateLimit:{setting}", StringComparison.Ordinal)).ShouldBeTrue();
    }
}

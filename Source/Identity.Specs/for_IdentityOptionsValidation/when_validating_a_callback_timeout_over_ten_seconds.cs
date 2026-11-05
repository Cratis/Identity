// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_IdentityOptionsValidation;

public class when_validating_a_callback_timeout_over_ten_seconds : given.a_callback_configuration
{
    [Theory]
    [InlineData(10001)]
    [InlineData(0)]
    [InlineData(-1)]
    void should_refuse_invalid_deadlines(int milliseconds)
    {
        options.Resources[0].TenantsCallback.Timeout = TimeSpan.FromMilliseconds(milliseconds);
        Validate();
        result.Failed.ShouldBeTrue();
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Cratis.Identity.given;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;

namespace Cratis.Identity.for_IdentityOptionsValidation;

public class when_the_read_model_catch_up_timeout_is_not_positive : Specification
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void should_refuse_the_configuration(int milliseconds)
    {
        var options = Configurations.TwoResourceServers();
        options.Accounts.ReadModelCatchUpTimeout = TimeSpan.FromMilliseconds(milliseconds);
        var validation = new IdentityOptionsValidation(new HostingEnvironment { EnvironmentName = Environments.Development });
        validation.Validate(null, options).Failed.ShouldBeTrue();
    }
}

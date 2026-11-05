// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Cratis.Identity.given;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.for_IdentityOptionsValidation.given;

public class a_callback_configuration : Specification
{
    protected IdentityOptions options;
    protected HostingEnvironment environment;
    protected ValidateOptionsResult result;

    void Establish()
    {
        options = Configurations.CallbackResources();
        environment = new HostingEnvironment { EnvironmentName = "Production" };
    }

    protected void Validate() => result = new IdentityOptionsValidation(environment).Validate(null, options);
}

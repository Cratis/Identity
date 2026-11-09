// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Cratis.Identity.given;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.for_TokenExchangeOptionsValidation.given;

public class a_token_exchange_configuration : Specification
{
    protected IdentityOptions options;
    protected string environmentName = "Production";
    protected ValidateOptionsResult result;

    void Establish() => options = Configurations.WithTokenExchange();

    protected ExternalIssuerOptions Issuer => options.TokenExchange.Issuers[0];
    protected TokenExchangeClientOptions Client => options.TokenExchange.Clients[0];

    protected void Validate() => result = new IdentityOptionsValidation(new HostingEnvironment { EnvironmentName = environmentName }).Validate(null, options);
}

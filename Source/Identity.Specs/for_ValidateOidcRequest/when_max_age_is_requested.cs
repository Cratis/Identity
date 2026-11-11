// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;
using Cratis.Identity.Configuration;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.for_ValidateOidcRequest;

public class when_max_age_is_requested : Specification
{
    List<ValidateAuthorizationRequestContext> _contexts = [];

    async Task Because()
    {
        var options = new IdentityOptions();
        options.Oidc.IssuanceEnabled = true;
        options.OidcClients.Add(new OidcClientOptions { ClientId = "oidc-web" });
        var handler = new ValidateOidcRequest(Options.Create(options));
        foreach (var value in new[] { "0", "300", "-1", "invalid", "" })
        {
            var context = new ValidateAuthorizationRequestContext(new OpenIddictServerTransaction
            {
                Options = new(),
                Request = new OpenIddictRequest(new Dictionary<string, string?> { ["max_age"] = value })
                {
                    ClientId = "oidc-web", Scope = "openid", Nonce = "request-nonce"
                },
                Response = new()
            }) { ClientId = "oidc-web" };
            await handler.HandleAsync(context);
            _contexts.Add(context);
        }
    }

    [Fact] void should_refuse_every_value_before_login_or_consent() => _contexts.All(context => context.IsRejected).ShouldBeTrue();
    [Fact] void should_report_invalid_request() => _contexts.All(context => context.Error == "invalid_request").ShouldBeTrue();
    [Fact] void should_explain_that_max_age_is_unsupported() => _contexts.All(context => context.ErrorDescription == "max_age is not supported").ShouldBeTrue();
}

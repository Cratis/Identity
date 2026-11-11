// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;
using Cratis.Identity.Authorization.given;
using Cratis.Identity.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.for_ValidateOidcRequest;

public class when_max_age_is_absent : Specification
{
    List<ValidateAuthorizationRequestContext> _contexts = [];

    async Task Because()
    {
        var options = new IdentityOptions();
        options.Oidc.IssuanceEnabled = true;
        options.OidcClients.Add(new OidcClientOptions { ClientId = "oidc-web" });
        var handler = new ValidateOidcRequest(Options.Create(options));
        foreach (var http in MaxAgeRequests.WithoutMaxAge())
        {
            var transaction = new OpenIddictServerTransaction
            {
                Options = new(),
                Request = new OpenIddictRequest { ClientId = "oidc-web", Scope = "openid", Nonce = "request-nonce" },
                Response = new()
            };
            transaction.SetProperty(typeof(HttpRequest).FullName, new WeakReference<HttpRequest>(http.Request));
            var context = new ValidateAuthorizationRequestContext(transaction);
            await handler.HandleAsync(context);
            _contexts.Add(context);
        }
    }

    [Fact] void should_leave_a_valid_identity_request_unchanged() => _contexts.TrueForAll(context => !context.IsRejected).ShouldBeTrue();
    [Fact] void should_not_supply_an_error() => _contexts.TrueForAll(context => context.Error is null).ShouldBeTrue();
}

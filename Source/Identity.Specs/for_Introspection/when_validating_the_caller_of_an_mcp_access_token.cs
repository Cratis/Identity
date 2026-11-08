// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Integration.given;
using Microsoft.Extensions.Logging.Abstractions;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.for_Introspection;

public class when_validating_the_caller_of_an_mcp_access_token : Specification
{
    ValidateIntrospectionRequestContext _mcp;
    ValidateIntrospectionRequestContext _api;

    async Task Because()
    {
        var handler = new OpenIddictServerHandlers.Introspection.ValidateAuthorizedParty();
        _mcp = Context(ResourceSettings.IntrospectionClientId);
        _api = Context("direct-v1");
        await handler.HandleAsync(_mcp);
        await handler.HandleAsync(_api);
    }

    [Fact] void should_allow_the_configured_mcp_introspection_client() => _mcp.IsRejected.ShouldBeFalse();
    [Fact] void should_reject_the_unrelated_api_client() => _api.IsRejected.ShouldBeTrue();
    [Fact] void should_report_an_invalid_token_to_the_unrelated_client() => _api.Error.ShouldEqual(Errors.InvalidToken);

    static ValidateIntrospectionRequestContext Context(string clientId) => new(new OpenIddictServerTransaction
    {
        Logger = NullLogger.Instance,
        Options = new OpenIddictServerOptions(),
        Request = new OpenIddictRequest { ClientId = clientId }
    })
    {
        GenericTokenPrincipal = new ClaimsPrincipal(new ClaimsIdentity("test"))
            .SetTokenType(TokenTypeIdentifiers.AccessToken)
            .SetAudiences("direct-api")
            .SetPresenters("cratis-cli")
    };
}

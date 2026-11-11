// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Microsoft.Extensions.Logging.Abstractions;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.for_Introspection;

public class when_validating_direct_resource_audiences : Specification
{
    [Theory]
    [InlineData("direct-mcp", "direct-api")]
    [InlineData("direct-api", "direct-mcp")]
    public async Task should_refuse_a_token_addressed_to_the_other_resource(string audience, string clientId)
    {
        var context = await Validate(audience, clientId);

        context.IsRejected.ShouldBeTrue();
        context.Error.ShouldEqual(Errors.InvalidToken);
    }

    [Theory]
    [InlineData("direct-mcp")]
    [InlineData("direct-api")]
    public async Task should_accept_a_token_addressed_to_the_calling_resource(string audience)
    {
        var context = await Validate(audience, audience);

        context.IsRejected.ShouldBeFalse();
    }

    static async Task<ValidateIntrospectionRequestContext> Validate(string audience, string clientId)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity("synthetic-access-token"))
            .SetTokenType(TokenTypeIdentifiers.AccessToken)
            .SetAudiences(audience)
            .SetPresenters("cratis-cli");
        var context = new ValidateIntrospectionRequestContext(new OpenIddictServerTransaction
        {
            Logger = NullLogger.Instance,
            Options = new OpenIddictServerOptions(),
            Request = new OpenIddictRequest { ClientId = clientId },
            Response = new OpenIddictResponse()
        })
        {
            GenericTokenPrincipal = principal
        };
        await new OpenIddictServerHandlers.Introspection.ValidateAuthorizedParty().HandleAsync(context);
        return context;
    }
}

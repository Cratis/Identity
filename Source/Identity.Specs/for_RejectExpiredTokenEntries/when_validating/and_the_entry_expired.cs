// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.given;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.for_RejectExpiredTokenEntries.when_validating;

public class and_the_entry_expired : given.a_token_to_validate
{
    List<ValidateTokenContext> _contexts;
    ValidateTokenContext _introspection;

    void Establish() => Expires(KeyMaterial.Now.AddSeconds(-1));

    async Task Because()
    {
        _contexts = [];
        foreach (var type in new[] { TokenTypeIdentifiers.Private.AuthorizationCode, TokenTypeIdentifiers.RefreshToken, TokenTypeIdentifiers.Private.DeviceCode, TokenTypeIdentifiers.Private.UserCode })
        {
            var context = Context();
            context.Principal = new ClaimsPrincipal(new ClaimsIdentity("test")).SetTokenType(type);
            await _handler.HandleAsync(context);
            _contexts.Add(context);
        }

        _introspection = Context(OpenIddictServerEndpointType.Introspection);
        await _handler.HandleAsync(_introspection);
    }

    [Fact] void should_reject_every_token_type_with_invalid_grant_at_the_token_endpoint() => _contexts.TrueForAll(_ => _.Error == Errors.InvalidGrant).ShouldBeTrue();
    [Fact] void should_reject_with_invalid_token_elsewhere() => _introspection.Error.ShouldEqual(Errors.InvalidToken);
}

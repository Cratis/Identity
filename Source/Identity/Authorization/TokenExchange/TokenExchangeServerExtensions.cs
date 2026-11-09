// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using OpenIddict.Server;

namespace Cratis.Identity.Authorization.TokenExchange;

/// <summary>
/// Enables the RFC 8693 grant on the OpenIddict server, and only when at least one client is configured.
/// </summary>
internal static class TokenExchangeServerExtensions
{
    /// <summary>
    /// Enables the built-in token-exchange grant with Identity's validation of external subject tokens.
    /// </summary>
    /// <param name="options">The <see cref="OpenIddictServerBuilder"/>.</param>
    /// <param name="identity">The startup <see cref="IdentityOptions"/>.</param>
    /// <returns>The builder for continuation.</returns>
    internal static OpenIddictServerBuilder AddTokenExchangeFlow(this OpenIddictServerBuilder options, IdentityOptions identity)
    {
        if (identity.TokenExchange.Clients.Count == 0)
        {
            return options;
        }

        // External tokens are validated by SubjectTokenValidator in TokenExchangeGrant, not with Identity's keys.
        return options
            .AllowTokenExchangeFlow()
            .RemoveEventHandler(OpenIddictServerHandlers.ValidateSubjectToken.Descriptor)
            .Configure(server =>
            {
                server.SubjectTokenTypes.Clear();
                server.SubjectTokenTypes.UnionWith([TokenExchangeConstants.JwtTokenType, TokenExchangeConstants.AccessTokenType]);
                server.ActorTokenTypes.Clear();
                server.RequestedTokenTypes.Clear();
                server.RequestedTokenTypes.Add(TokenExchangeConstants.AccessTokenType);
            });
    }
}

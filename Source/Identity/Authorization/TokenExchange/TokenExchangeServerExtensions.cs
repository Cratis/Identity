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
    /// Enables the token-exchange grant as a custom flow (OpenIddict's built-in flow validates only Identity's own tokens)
    /// and adds <c language="text">issued_token_type</c> to successful exchange responses.
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

        return options
            .AllowCustomFlow(TokenExchangeConstants.GrantType)
            .AddEventHandler<OpenIddictServerEvents.ApplyTokenResponseContext>(builder => builder.UseInlineHandler(context =>
            {
                if (string.IsNullOrEmpty(context.Error) && string.Equals(context.Request?.GrantType, TokenExchangeConstants.GrantType, StringComparison.Ordinal))
                {
                    context.Response.SetParameter(TokenExchangeConstants.IssuedTokenType, TokenExchangeConstants.AccessTokenType);
                }

                return default;
            }));
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Authorization;

/// <summary>
/// Holds log messages for <see cref="AuthorizationEndpoint"/>.
/// </summary>
internal static partial class AuthorizationEndpointLogging
{
    [LoggerMessage(LogLevel.Information, "Account {AccountId} has no tenant it can select for {Audience}")]
    internal static partial void NoSelectableTenant(this ILogger<AuthorizationEndpoint> logger, string accountId, string audience);

    [LoggerMessage(LogLevel.Information, "Account {AccountId} denied client {ClientId}")]
    internal static partial void ConsentDenied(this ILogger<AuthorizationEndpoint> logger, string accountId, string clientId);

    [LoggerMessage(LogLevel.Information, "Account {AccountId} authorized client {ClientId} for {Audience} in tenant {TenantId} with scopes {Scopes}")]
    internal static partial void ConsentGranted(this ILogger<AuthorizationEndpoint> logger, string accountId, string clientId, string audience, string tenantId, string scopes);
}

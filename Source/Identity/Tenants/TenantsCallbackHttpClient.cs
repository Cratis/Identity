// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Tenants;

/// <summary>
/// Defines the isolated product callback client and its transport restrictions.
/// </summary>
internal static class TenantsCallbackHttpClient
{
    /// <summary>
    /// The named client used for product callbacks.
    /// </summary>
    internal const string Name = "Cratis.Identity.TenantsCallback";

    /// <summary>
    /// Creates a handler that never forwards credentials through redirects or cookies and uses default TLS validation.
    /// </summary>
    /// <returns>The restricted handler.</returns>
    internal static SocketsHttpHandler CreateHandler() => new()
    {
        AllowAutoRedirect = false,
        UseCookies = false,
        ConnectTimeout = TimeSpan.FromSeconds(2),
        PooledConnectionLifetime = TimeSpan.FromMinutes(2)
    };
}

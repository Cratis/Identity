// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.for_McpAcceptance.given;

/// <summary>
/// Supplies realm-specific transport and registration for the acceptance flow; Identity#126 can supply a deployed realm.
/// </summary>
public interface IMcpAcceptanceHarness
{
    Uri Issuer { get; }
    string ClientId { get; }
    string RedirectUri { get; }
    string Tenant { get; }
    McpProduct Direct { get; }
    McpProduct Studio { get; }

    /// <summary>
    /// Creates a cookie-preserving browser with a first sign-in strategy.
    /// </summary>
    /// <returns>The browser for both product authorizations.</returns>
    Browser CreateBrowser();

    /// <summary>
    /// Makes another upstream sign-in fail, so the second product must use the existing Identity session.
    /// </summary>
    /// <param name="browser">The browser that completed the first authorization.</param>
    void PreventFurtherSignIns(Browser browser);

    /// <summary>
    /// Creates a client authorized as the product's introspection client.
    /// </summary>
    /// <param name="product">The product whose resource server introspects.</param>
    /// <param name="handler">A handler placed in front of Identity's introspection endpoint.</param>
    /// <returns>The authorized client.</returns>
    HttpClient CreateIntrospectionClient(McpProduct product, DelegatingHandler handler);

    /// <summary>
    /// Revokes a grant by revoking its refresh token as the registered client.
    /// </summary>
    /// <param name="browser">The Identity transport.</param>
    /// <param name="refreshToken">The grant's refresh token.</param>
    /// <returns>The revocation endpoint's status code.</returns>
    Task<HttpStatusCode> Revoke(Browser browser, string refreshToken);
}

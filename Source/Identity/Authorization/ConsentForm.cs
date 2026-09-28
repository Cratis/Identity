// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Authorization;

/// <summary>
/// Holds the names and values of the consent form fields.
/// </summary>
public static class ConsentForm
{
    /// <summary>
    /// The field carrying the user decision.
    /// </summary>
    public const string Decision = "consent_decision";

    /// <summary>
    /// The field carrying the selected tenant.
    /// </summary>
    public const string Tenant = "consent_tenant";

    /// <summary>
    /// The field carrying the antiforgery token.
    /// </summary>
    public const string AntiforgeryField = "__consent_antiforgery";

    /// <summary>
    /// The decision value that grants access.
    /// </summary>
    public const string Allow = "allow";

    /// <summary>
    /// The decision value that denies access.
    /// </summary>
    public const string Deny = "deny";
}

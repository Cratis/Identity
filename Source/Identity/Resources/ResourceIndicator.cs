// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Resources;

/// <summary>
/// Normalizes RFC 8707 resource indicators so configured and requested values compare the same way
/// OpenIddict compares them.
/// </summary>
public static class ResourceIndicator
{
    /// <summary>
    /// Normalizes a resource indicator.
    /// </summary>
    /// <param name="indicator">The indicator to normalize.</param>
    /// <returns>The absolute URI form of the indicator, or <see langword="null"/> if it is not an absolute URI without a fragment.</returns>
    public static string? Normalize(string indicator) =>
        Uri.TryCreate(indicator, UriKind.Absolute, out var uri) && string.IsNullOrEmpty(uri.Fragment) ? uri.AbsoluteUri : null;
}

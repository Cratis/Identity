// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Identity.Contracts.Clients;

/// <summary>An absolute HTTP redirect without user information, query or fragment; never a signed or redeemable URL.</summary>
/// <param name="Value">The redirect.</param>
public record RedirectUri(string Value) : ConceptAs<string>(Value)
{
    /// <summary>Validates a configured redirect before it becomes a fact.</summary>
    /// <param name="value">The configured redirect.</param>
    /// <returns>The validated redirect.</returns>
    /// <exception cref="InvalidClientValue">The redirect is invalid.</exception>
    public static RedirectUri Create(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length > 2048 ||
            !Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https") ||
            !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
        {
            throw new InvalidClientValue("redirect uri");
        }

        return new(value);
    }
}

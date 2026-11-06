// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Identity.Contracts.Clients;

/// <summary>A validated, non-redeemable OAuth client identifier.</summary>
/// <param name="Value">The identifier.</param>
public record ClientId(string Value) : ConceptAs<string>(Value)
{
    /// <summary>Validates a printable ASCII identifier without whitespace.</summary>
    /// <param name="value">The configured identifier.</param>
    /// <returns>The validated identifier.</returns>
    /// <exception cref="InvalidClientValue">The identifier is invalid.</exception>
    public static ClientId Create(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length > 255 || value.Any(character => character is < '!' or > '~'))
        {
            throw new InvalidClientValue("client id");
        }

        return new(value);
    }
}

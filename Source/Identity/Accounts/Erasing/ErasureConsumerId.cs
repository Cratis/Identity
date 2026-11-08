// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Identity.Accounts.Erasing;

/// <summary>A stable, non-personal name of a downstream consumer of the public account events.</summary>
/// <param name="Value">The consumer name.</param>
internal record ErasureConsumerId(string Value) : ConceptAs<string>(Value)
{
    /// <summary>Converts a consumer name.</summary>
    /// <param name="value">The name.</param>
    public static implicit operator ErasureConsumerId(string value) => new(value);
}

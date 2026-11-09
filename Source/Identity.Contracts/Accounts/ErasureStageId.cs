// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Identity.Contracts.Accounts;

/// <summary>An allowlisted name of an erasure workflow stage, never free text.</summary>
/// <param name="Value">The stage name.</param>
public record ErasureStageId(string Value) : ConceptAs<string>(Value)
{
    /// <summary>Converts a stage name.</summary>
    /// <param name="value">The name.</param>
    public static implicit operator ErasureStageId(string value) => new(value);
}

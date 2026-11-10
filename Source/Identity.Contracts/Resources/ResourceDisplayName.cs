// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Identity.Contracts.Resources;

/// <summary>A reviewed resource business label, never an account name or personal identifier.</summary>
/// <param name="Value">The non-person business label.</param>
public record ResourceDisplayName(string Value) : ConceptAs<string>(Value);

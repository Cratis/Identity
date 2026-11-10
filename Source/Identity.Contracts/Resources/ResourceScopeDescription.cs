// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Identity.Contracts.Resources;

/// <summary>A non-person business description of a resource scope, retained only on Internal facts.</summary>
/// <param name="Value">The scope description.</param>
public record ResourceScopeDescription(string Value) : ConceptAs<string>(Value);

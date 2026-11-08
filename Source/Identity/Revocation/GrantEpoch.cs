// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Identity.Revocation;

/// <summary>The irreversible epoch of this grant, not an account or realm epoch.</summary>
/// <param name="Value">The epoch.</param>
internal sealed record GrantEpoch(ulong Value) : ConceptAs<ulong>(Value);

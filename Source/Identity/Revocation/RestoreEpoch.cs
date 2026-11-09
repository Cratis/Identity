// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Identity.Revocation;

/// <summary>The independently held realm recovery epoch, not a policy epoch.</summary>
/// <param name="Value">Monotonic restore epoch.</param>
internal sealed record RestoreEpoch(ulong Value) : ConceptAs<ulong>(Value);

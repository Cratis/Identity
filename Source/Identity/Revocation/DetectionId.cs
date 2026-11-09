// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Identity.Revocation;

/// <summary>A persisted reuse observation identity, never a submitted value.</summary>
/// <param name="Value">The UUID.</param>
internal sealed record DetectionId(Guid Value) : ConceptAs<Guid>(Value);

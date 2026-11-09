// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Identity.Revocation;

/// <summary>A non-secret grant/family UUID derived from its operational identifier.</summary>
/// <param name="Value">The UUID.</param>
internal sealed record GrantId(Guid Value) : ConceptAs<Guid>(Value);

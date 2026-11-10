// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Resources;

namespace Cratis.Identity.Resources;

/// <summary>A registered scope, including its permanent retirement tombstone.</summary>
/// <param name="ScopeName">The full exact scope name.</param>
/// <param name="Description">The Internal business description.</param>
/// <param name="Retired">Whether the scope is permanently retired.</param>
/// <param name="RetiredAt">The retirement instant, if retired.</param>
internal sealed record ResourceScopeRegistration(string ScopeName, ResourceScopeDescription Description, bool Retired = false, DateTimeOffset? RetiredAt = null);

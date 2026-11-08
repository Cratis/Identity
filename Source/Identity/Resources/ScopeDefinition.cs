// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Resources;

/// <summary>
/// Represents a scope a resource server accepts.
/// </summary>
/// <param name="Name">The scope name.</param>
/// <param name="Description">The description shown on the consent page.</param>
internal record ScopeDefinition(string Name, string Description);

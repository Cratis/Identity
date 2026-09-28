// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Resources;

namespace Cratis.Identity.Authorization;

/// <summary>
/// Represents what Identity is willing to grant for an authorization request, before the user consents.
/// </summary>
/// <param name="Resource">The single <see cref="ResourceServer"/> the grant is bound to.</param>
/// <param name="Scopes">The scopes that will be granted: the requested scopes the resource server owns, plus requested protocol scopes.</param>
public record Grant(ResourceServer Resource, IEnumerable<string> Scopes);

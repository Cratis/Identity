// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Authorization;

/// <summary>
/// Represents a <see cref="GrantRequest"/> that can be granted.
/// </summary>
/// <param name="Grant">The <see cref="Authorization.Grant"/>.</param>
internal sealed record Grantable(Grant Grant) : GrantEvaluation;

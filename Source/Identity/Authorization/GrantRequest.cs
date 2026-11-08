// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Authorization;

/// <summary>
/// Represents what a client asked for in an authorization request.
/// </summary>
/// <param name="Scopes">The requested scopes.</param>
/// <param name="ResourceIndicators">The requested RFC 8707 resource indicators.</param>
internal record GrantRequest(IEnumerable<string> Scopes, IEnumerable<string> ResourceIndicators);

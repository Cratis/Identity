// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Authorization;

/// <summary>
/// Represents a request that cannot be granted, with the OAuth error to return to the client.
/// </summary>
/// <param name="Error">The OAuth error code.</param>
/// <param name="Description">The error description.</param>
public sealed record NotGrantable(string Error, string Description) : GrantEvaluation;

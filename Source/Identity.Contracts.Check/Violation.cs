// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Contracts.Check;

/// <summary>A value-free content-rule diagnostic.</summary>
/// <param name="Path">The member path.</param>
/// <param name="ReasonId">The stable rejection reason.</param>
internal sealed record Violation(string Path, string ReasonId);

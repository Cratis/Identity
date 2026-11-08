// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Contracts.Check;

/// <summary>A source-fact mapping from catalogue section 4.1.</summary>
/// <param name="Contract">The published contract.</param>
/// <param name="Name">The internal source fact.</param>
/// <param name="TypeId">The allocated identity or a placeholder.</param>
internal sealed record SourceFactRow(string Contract, string Name, string? TypeId);

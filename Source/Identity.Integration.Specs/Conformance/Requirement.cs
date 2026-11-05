// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.Conformance;

/// <summary>
/// A protocol obligation traced to its normative reference.
/// </summary>
/// <param name="Id">The stable requirement identifier.</param>
/// <param name="Reference">The normative reference.</param>
/// <param name="Level">The normative requirement level.</param>
/// <param name="Summary">The behavior to prove.</param>
public record Requirement(string Id, string Reference, string Level, string Summary);

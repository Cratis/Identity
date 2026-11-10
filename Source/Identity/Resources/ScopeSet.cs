// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Identity.Resources;

/// <summary>A space-delimited set of protocol scopes.</summary>
/// <param name="Value">The stored scope string.</param>
internal sealed record ScopeSet(string Value) : ConceptAs<string>(Value)
{
    /// <summary>Gets the individual scopes, excluding empty entries.</summary>
    internal IEnumerable<Scope> Scopes => Value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(_ => new Scope(_));

    /// <summary>Orders scopes ordinally for a canonical selection.</summary>
    /// <param name="scopes">The selected scopes.</param>
    /// <returns>The canonical scope string.</returns>
    internal static ScopeSet From(IEnumerable<Scope> scopes) => new(string.Join(' ', scopes.Select(_ => _.Value).Order(StringComparer.Ordinal)));

    /// <summary>Preserves a primitive scope string.</summary>
    /// <param name="value">The stored scopes.</param>
    public static implicit operator ScopeSet(string value) => new(value);
}

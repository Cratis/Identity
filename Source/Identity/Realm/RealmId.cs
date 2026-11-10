// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Identity.Realm;

/// <summary>The identifier of a realm.</summary>
/// <param name="Value">The realm identifier.</param>
internal sealed record RealmId(string Value) : ConceptAs<string>(Value)
{
    /// <summary>Converts a primitive realm identifier.</summary>
    /// <param name="value">The realm identifier.</param>
    public static implicit operator RealmId(string value) => new(value);
}

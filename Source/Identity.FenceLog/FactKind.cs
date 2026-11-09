// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Revocation;

/// <summary>Closed source-fact kinds accepted by the dispatcher.</summary>
public enum FactKind
{
    /// <summary>A grant restriction.</summary>
    AuthorizationRevocation = 0,

    /// <summary>A family restriction.</summary>
    FamilyRevocation = 1,

    /// <summary>The first family admission.</summary>
    FamilyStarted = 2,

    /// <summary>A repeated presentation.</summary>
    Reuse = 3
}

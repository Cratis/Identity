// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Realm;

/// <summary>Pins namespace resolution before any realm collaborator is resolved.</summary>
internal sealed class IdentityRealmScope : IDisposable
{
    static readonly AsyncLocal<bool> _active = new();
    readonly bool _previous = _active.Value;

    IdentityRealmScope() => _active.Value = true;

    /// <summary>Gets whether the current flow belongs to the Identity realm.</summary>
    internal static bool IsActive => _active.Value;

    /// <inheritdoc/>
    public void Dispose() => _active.Value = _previous;

    /// <summary>Begins realm dispatch, restoring the previous flow when disposed.</summary>
    /// <returns>The scope to dispose.</returns>
    internal static IDisposable Begin() => new IdentityRealmScope();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Protocol;

/// <summary>Marks the verified protocol dispatch flow without exposing an activation API.</summary>
internal sealed class ProtocolCommandScope : IDisposable
{
    static readonly AsyncLocal<bool> _active = new();
    readonly bool _previous = _active.Value;

    ProtocolCommandScope() => _active.Value = true;

    /// <summary>Gets whether this flow originates at the protocol boundary.</summary>
    internal static bool IsActive => _active.Value;

    /// <inheritdoc/>
    public void Dispose() => _active.Value = _previous;

    /// <summary>Begins protocol dispatch, restoring the previous flow when disposed.</summary>
    /// <returns>The scope to dispose.</returns>
    internal static IDisposable Begin() => new ProtocolCommandScope();
}

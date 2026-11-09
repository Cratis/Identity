// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Identity.Protocol;

/// <summary>The execution boundary for commands authenticated by an Identity protocol.</summary>
internal interface IProtocolCommands
{
    /// <summary>Executes in an independent dependency scope pinned to the Identity realm.</summary>
    /// <param name="command">The protocol command.</param>
    /// <param name="prepare">Optional initialization of verified protocol state in the child scope.</param>
    /// <param name="cancellationToken">The request cancellation, observed while waiting for read-model catch-up.</param>
    /// <returns>The command result.</returns>
    Task<CommandResult> Execute(object command, Action<IServiceProvider>? prepare = null, CancellationToken cancellationToken = default);

    /// <summary>Waits for filtered projection progress in the realm before the single bounded retry.</summary>
    /// <param name="cancellationToken">The request cancellation, observed while waiting.</param>
    /// <returns>The projection progress fence.</returns>
    Task BeforeRetry(CancellationToken cancellationToken = default);
}

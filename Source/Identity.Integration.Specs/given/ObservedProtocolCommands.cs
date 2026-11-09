// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Identity.Protocol;

namespace Cratis.Identity.Integration.given;

/// <summary>
/// Observes real protocol results and retry fences without replacing append or catch-up behavior.
/// </summary>
/// <param name="inner">The real protocol boundary.</param>
/// <param name="interleaving">The async-flow-local observation.</param>
internal sealed class ObservedProtocolCommands(ProtocolCommands inner, NativeSignInInterleaving interleaving) : IProtocolCommands
{
    /// <inheritdoc/>
    public async Task<CommandResult> Execute(object command, Action<IServiceProvider>? prepare = null, CancellationToken cancellationToken = default)
    {
        var result = await inner.Execute(command, prepare, cancellationToken);
        interleaving.Completed(command, result);
        if (result.HasExceptions)
        {
            await Console.Error.WriteLineAsync($"[spec-diagnostic] {command.GetType().Name} failed: {string.Join(" | ", result.ExceptionMessages)}\n{result.ExceptionStackTrace}");
        }

        return result;
    }

    /// <inheritdoc/>
    public Task BeforeRetry(CancellationToken cancellationToken = default)
    {
        interleaving.Retrying();
        return inner.BeforeRetry(cancellationToken);
    }
}

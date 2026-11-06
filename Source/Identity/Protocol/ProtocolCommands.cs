// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Arc.Commands;
using Cratis.Chronicle.EventSequences;
using Cratis.Identity.Realm;

namespace Cratis.Identity.Protocol;

/// <summary>Executes verified commands through Arc in a child scope owned by the realm.</summary>
/// <param name="scopes">The dependency scope factory.</param>
/// <param name="pipeline">The Arc pipeline.</param>
public sealed class ProtocolCommands(IServiceScopeFactory scopes, ICommandPipeline pipeline) : IProtocolCommands
{
    /// <inheritdoc/>
    public async Task BeforeRetry()
    {
        using var realm = IdentityRealmScope.Begin();
        await using var child = scopes.CreateAsyncScope();
        await child.ServiceProvider.GetRequiredService<ReadModelCatchUp>().BeforeRetry(CancellationToken.None);
    }

    /// <inheritdoc/>
    public async Task<CommandResult> Execute(object command, Action<IServiceProvider>? prepare = null)
    {
        using var protocol = ProtocolCommandScope.Begin();
        using var realm = IdentityRealmScope.Begin();
        await using var child = scopes.CreateAsyncScope();
        prepare?.Invoke(child.ServiceProvider);
        var appends = new List<AppendedEventWithResult>();
        using var capture = child.ServiceProvider.GetService<IEventLog>()?.AppendOperations.Subscribe(appends.AddRange);
        var result = await pipeline.Execute(command, child.ServiceProvider);
        if (result.IsSuccess && appends.Count != 0)
        {
            await child.ServiceProvider.GetRequiredService<ReadModelCatchUp>().AfterCommand(appends, CancellationToken.None);
        }

        return result;
    }
}

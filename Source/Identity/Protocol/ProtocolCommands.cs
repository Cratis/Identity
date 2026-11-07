// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Linq;
using Cratis.Arc.Commands;
using Cratis.Chronicle.EventSequences;
using Cratis.Execution;
using Cratis.Identity.Realm;

namespace Cratis.Identity.Protocol;

/// <summary>Executes verified commands through Arc in a child scope owned by the realm.</summary>
/// <param name="scopes">The dependency scope factory.</param>
/// <param name="pipeline">The Arc pipeline.</param>
internal sealed class ProtocolCommands(IServiceScopeFactory scopes, ICommandPipeline pipeline) : IProtocolCommands
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
        var appends = new ConcurrentQueue<AppendedEventWithResult>();
        CommandResult result;
        using (child.ServiceProvider.GetService<IEventLog>()?.AppendOperations.Subscribe(appended =>
        {
            foreach (var append in appended)
            {
                appends.Enqueue(append);
            }
        }))
        {
            // Arc generates a correlation id for a direct execution without installing it ambiently, so appends would carry another one.
            var accessor = child.ServiceProvider.GetService<ICorrelationIdAccessor>();
            var modifier = child.ServiceProvider.GetService<ICorrelationIdModifier>();
            var previous = accessor?.Current;
            modifier?.Modify(CorrelationId.New());
            try
            {
                result = await pipeline.Execute(command, child.ServiceProvider);
            }
            finally
            {
                if (previous is not null)
                {
                    modifier?.Modify(previous);
                }
            }
        }

        var ownAppends = appends.Where(append => append.Result.CorrelationId == result.CorrelationId).ToArray();
        if (result.IsSuccess && ownAppends.Length != 0)
        {
            await child.ServiceProvider.GetRequiredService<ReadModelCatchUp>().AfterCommand(ownAppends, CancellationToken.None);
        }

        return result;
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Cratis.Chronicle;

namespace Cratis.Identity.Realm;

/// <summary>Fails closed when a cached store was resolved outside the realm boundary.</summary>
/// <param name="logger">The logger.</param>
internal sealed partial class IdentityRealmNamespaceFilter(ILogger<IdentityRealmNamespaceFilter> logger) : ICommandFilter, IQueryFilter
{
    /// <inheritdoc/>
    public Task<CommandResult> OnExecution(CommandContext context) => Task.FromResult(
        IsIdentity(context.Type.Namespace) && IsMismatch(context.ServiceProvider)
            ? CommandResult.FromException(context.CorrelationId, new IdentityRealmNamespaceMismatch())
            : CommandResult.Success(context.CorrelationId));

    /// <inheritdoc/>
    public Task<QueryResult> OnPerform(QueryContext context) => Task.FromResult(
        IsIdentity(context.Name.Value) && IsMismatch(context.ServiceProvider)
            ? new QueryResult { CorrelationId = context.CorrelationId, ExceptionMessages = [new IdentityRealmNamespaceMismatch().Message] }
            : QueryResult.Success(context.CorrelationId));

    static bool IsIdentity(string? name) => name?.StartsWith("Cratis.Identity.", StringComparison.Ordinal) == true;

    [LoggerMessage(LogLevel.Error, "Identity operation refused outside the realm namespace")]
    static partial void RefusedRealmNamespace(ILogger logger);

    bool IsMismatch(IServiceProvider? services)
    {
        if (services?.GetService<IEventStore>()?.Namespace == EventStoreNamespaceName.Default)
        {
            return false;
        }

        RefusedRealmNamespace(logger);
        return true;
    }
}

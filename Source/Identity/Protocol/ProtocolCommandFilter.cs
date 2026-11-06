// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Commands;

namespace Cratis.Identity.Protocol;

/// <summary>Refuses direct execution of protocol-only commands, including validation requests.</summary>
public sealed class ProtocolCommandFilter : ICommandFilter
{
    /// <inheritdoc/>
    public Task<CommandResult> OnExecution(CommandContext context) => Task.FromResult(
        context.Type.IsDefined(typeof(ProtocolCommandAttribute)) && !ProtocolCommandScope.IsActive
            ? CommandResult.Unauthorized(context.CorrelationId, "protocol_only")
            : new CommandResult { CorrelationId = context.CorrelationId });
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Commands;

namespace Cratis.Identity.Integration.given;

/// <summary>Observes entry to the real native sign-in handler without changing its result.</summary>
/// <param name="inner">The real native sign-in handler.</param>
/// <param name="probe">The native entry counters.</param>
public sealed class ObservedSignInHandler(ICommandHandler inner, NativeCommandProbe probe) : ICommandHandler
{
    public IEnumerable<string> Location => inner.Location;
    public Type CommandType => inner.CommandType;
    public IEnumerable<Type> Dependencies => inner.Dependencies;
    public IEnumerable<ParameterInfo> Parameters => inner.Parameters;
    public bool AllowsAnonymousAccess => inner.AllowsAnonymousAccess;

    public ValueTask<object?> Handle(CommandContext commandContext)
    {
        probe.InvokingHandler();
        return inner.Handle(commandContext);
    }
}

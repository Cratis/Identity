// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Identity.Accounts.SigningIn;

namespace Cratis.Identity.Integration.given;

/// <summary>Decorates the real handler provider, retaining its discovery and authorization metadata.</summary>
/// <param name="inner">The real discovered command handlers.</param>
/// <param name="probe">The native entry counters.</param>
/// <param name="interleaving">The test-owned pre-append pause.</param>
public sealed class ObservedCommandHandlers(CommandHandlerProviders inner, NativeCommandProbe probe, NativeSignInInterleaving interleaving) : ICommandHandlerProviders
{
    public IEnumerable<ICommandHandler> Handlers => inner.Handlers.Select(Observe);

    public bool TryGetHandlerFor(object command, out ICommandHandler handler)
    {
        if (!inner.TryGetHandlerFor(command, out handler))
        {
            return false;
        }

        handler = Observe(handler);
        return true;
    }

    ICommandHandler Observe(ICommandHandler handler) => handler.CommandType == typeof(SignInWithUpstreamKey)
        ? new ObservedSignInHandler(handler, probe, interleaving)
        : handler;
}

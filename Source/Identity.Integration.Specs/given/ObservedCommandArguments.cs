// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Validation;
using Cratis.Identity.Accounts.SigningIn;

namespace Cratis.Identity.Integration.given;

/// <summary>Observes entry to argument resolution, which includes the native sign-in Provide method.</summary>
/// <param name="inner">The real argument resolver.</param>
/// <param name="probe">The native entry counters.</param>
public sealed class ObservedCommandArguments(CommandHandlerArgumentResolver inner, NativeCommandProbe probe) : ICommandHandlerArgumentResolver
{
    public ValueTask<CommandHandlerArgumentResolution> Resolve(ICommandHandler handler, CommandContext context, IServiceProvider serviceProvider, ValidationResultSeverity? allowedSeverity)
    {
        if (handler.CommandType == typeof(SignInWithUpstreamKey))
        {
            probe.ResolvingArguments();
        }

        return inner.Resolve(handler, context, serviceProvider, allowedSeverity);
    }
}

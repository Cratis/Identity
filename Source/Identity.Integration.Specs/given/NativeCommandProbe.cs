// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.given;

/// <summary>Counts native handler entry points without replacing authorization or command behavior.</summary>
public sealed class NativeCommandProbe
{
    int _argumentResolutions;
    int _handlerInvocations;

    public int ArgumentResolutions => Volatile.Read(ref _argumentResolutions);
    public int HandlerInvocations => Volatile.Read(ref _handlerInvocations);

    public void ResolvingArguments() => Interlocked.Increment(ref _argumentResolutions);
    public void InvokingHandler() => Interlocked.Increment(ref _handlerInvocations);
}

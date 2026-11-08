// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.RevocationSpecs;

/// <summary>Raised by an armed fault point after its effect is durable, simulating a crash.</summary>
/// <param name="point">The point that fired.</param>
internal sealed class InjectedFault(FaultPoint point) : Exception($"Injected fault {point}.")
{
    internal FaultPoint Point { get; } = point;
}

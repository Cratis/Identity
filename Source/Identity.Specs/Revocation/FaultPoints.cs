// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.RevocationSpecs;

/// <summary>Shared one-shot fault arming for the snapshotable fakes.</summary>
internal sealed class FaultPoints
{
    readonly HashSet<FaultPoint> _armed = [];

    internal void Arm(FaultPoint point)
    {
        lock (_armed)
        {
            _armed.Add(point);
        }
    }

    internal void Reach(FaultPoint point)
    {
        lock (_armed)
        {
            if (_armed.Remove(point))
            {
                throw new InjectedFault(point);
            }
        }
    }
}

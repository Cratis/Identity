// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Revocation;

/// <summary>Decides whether the fence log's deployment boundary is acceptable next to the operational one.</summary>
internal static class FenceLogBoundary
{
    /// <summary>Gets a value indicating whether the two deployments must be refused as one restore boundary.</summary>
    /// <param name="operationalDeployment">The operational store's deployment identity, for example its replica set id.</param>
    /// <param name="independentDeployment">The fence log's deployment identity.</param>
    /// <param name="options">The fence log settings, carrying the operator's explicit acceptance of a shared deployment.</param>
    /// <returns>True when the same deployment holds both stores and the operator has not accepted that.</returns>
    internal static bool Refuses(string operationalDeployment, string independentDeployment, FenceLogOptions options) =>
        operationalDeployment == independentDeployment && !options.AllowSharedDeployment;
}

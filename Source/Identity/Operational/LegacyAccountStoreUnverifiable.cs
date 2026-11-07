// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Operational;

/// <summary>The legacy MongoDB account store could not be checked, so accounts cannot be proven safe.</summary>
/// <param name="location">Either "configured" or "default"; never a host, credential or database name.</param>
/// <param name="reason">Why the check failed.</param>
public sealed class LegacyAccountStoreUnverifiable(string location, LegacyStoreFailure reason) : Exception(
    $"The legacy MongoDB account store at the {location} location could not be checked ({Describe(reason)}). " +
    "Identity refuses to start because it cannot prove no accounts would be lost. " +
    $"Make the MongoDB server reachable, or set {LegacyAccountStoreGuard.OptOutSetting}=true after confirming there is no legacy store.")
{
    static string Describe(LegacyStoreFailure reason) => reason switch
    {
        LegacyStoreFailure.Timeout => "timeout",
        LegacyStoreFailure.Unreachable => "unreachable",
        LegacyStoreFailure.AuthenticationFailed => "authentication failed",
        _ => "error"
    };
}

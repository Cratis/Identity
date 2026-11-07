// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Operational;

/// <summary>A legacy MongoDB account collection with documents exists; starting would make those accounts disappear.</summary>
/// <param name="location">Either "configured" or "default"; never a host, credential or database name.</param>
public sealed class LegacyAccountStoreFound(string location) : Exception(
    $"A legacy MongoDB account store with existing accounts was found in the {location} Identity database (collection 'accounts'). " +
    "Identity now stores accounts in Chronicle and does NOT migrate or delete MongoDB account data, so starting would make those accounts disappear. " +
    "Nothing was changed. Export or retire the legacy 'accounts' collection yourself, or, if you have confirmed the data is obsolete, " +
    $"set {LegacyAccountStoreGuard.OptOutSetting}=true to start.");

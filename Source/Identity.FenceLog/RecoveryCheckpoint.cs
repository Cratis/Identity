// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Revocation;

/// <summary>Restorable operational progress, bound to independent custody.</summary>
/// <param name="InstanceId">Independent custody identity.</param>
/// <param name="AppliedThrough">The complete operational prefix.</param>
/// <param name="Nonce">The mirrored recovery-owner nonce.</param>
public sealed record RecoveryCheckpoint(string InstanceId, long AppliedThrough, string Nonce);

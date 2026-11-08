// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Revocation;

/// <summary>Independent progress and coverage proof, never restored with either consumer.</summary>
/// <param name="InstanceId">Independent custody identity.</param>
/// <param name="AppliedThrough">The last independently witnessed applied prefix.</param>
/// <param name="Nonce">The mirrored recovery-owner nonce.</param>
/// <param name="Epoch">The independently enforced epoch.</param>
/// <param name="ClearedEpoch">The epoch whose uncovered restrictions were independently reconciled.</param>
internal sealed record RecoveryCustody(string InstanceId, long AppliedThrough, string Nonce, long Epoch, long ClearedEpoch);

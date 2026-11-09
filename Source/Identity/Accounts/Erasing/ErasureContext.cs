// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.Erasing;

/// <summary>What a stage needs to erase one subject; references only, never personal data.</summary>
/// <param name="Account">The account subject.</param>
/// <param name="ErasureId">The accepted request.</param>
/// <param name="Namespaces">Every tenant or realm namespace the erasure covers.</param>
internal record ErasureContext(AccountSubject Account, ErasureId ErasureId, IReadOnlyList<string> Namespaces);

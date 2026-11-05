// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Accounts.Linking;

/// <summary>Raw-source projection guarding every fact read by the account reducer.</summary>
/// <param name="Id">The immutable account source.</param>
public record AccountGuard(string Id);

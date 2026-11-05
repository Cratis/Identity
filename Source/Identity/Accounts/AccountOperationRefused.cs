// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Accounts;

/// <summary>A fail-closed account operation, containing only an allowlisted reason.</summary>
/// <param name="reason">The reason code, never evidence or personal content.</param>
public class AccountOperationRefused(string reason) : Exception(reason);

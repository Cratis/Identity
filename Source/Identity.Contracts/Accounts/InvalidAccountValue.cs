// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Contracts.Accounts;

/// <summary>
/// The exception that is thrown when an account value violates its canonical format.
/// </summary>
/// <param name="kind">The kind of value, never its personal content.</param>
public class InvalidAccountValue(string kind) : Exception($"Invalid {kind}.");

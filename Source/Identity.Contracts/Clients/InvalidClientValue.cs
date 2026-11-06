// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Contracts.Clients;

/// <summary>The exception thrown when a client value violates its format.</summary>
/// <param name="kind">The kind of value, never its content.</param>
public class InvalidClientValue(string kind) : Exception($"Invalid {kind}.");

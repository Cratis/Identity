// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Keys;

/// <summary>
/// The exception that is thrown when no key of a purpose is valid now. Identity refuses to start, or keeps the
/// previous keys on reload, rather than issuing tokens it cannot sign or protect.
/// </summary>
/// <param name="purpose">The <see cref="KeyPurpose"/> without a usable key.</param>
internal class NoUsableKey(KeyPurpose purpose) : Exception($"The secret store holds no {purpose.ToString().ToLowerInvariant()} key that is valid now.");

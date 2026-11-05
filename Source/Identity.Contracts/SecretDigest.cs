// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Identity.Contracts;

/// <summary>A keyed HMAC-SHA-256 digest, v&lt;revision&gt;.&lt;base64url&gt;, produced with a separate secret-custody key; never a secret.</summary>
/// <param name="Value">The revision-prefixed digest.</param>
public record SecretDigest(string Value) : ConceptAs<string>(Value);

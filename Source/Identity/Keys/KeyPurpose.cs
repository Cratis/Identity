// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Keys;

/// <summary>
/// Represents what a realm key is used for. A key serves exactly one purpose.
/// </summary>
internal enum KeyPurpose
{
    /// <summary>
    /// The key signs issued tokens and its public half is published through JWKS.
    /// </summary>
    Signing = 0,

    /// <summary>
    /// The key encrypts authorization codes and refresh tokens.
    /// </summary>
    Encryption = 1
}

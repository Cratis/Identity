// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.given;

/// <summary>
/// An <see cref="IdentityHost"/> that only the erasure replay specs use; it owns its own kernel and MongoDB containers.
/// </summary>
public class ErasureReplayHost : IdentityHost;

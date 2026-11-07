// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Revocation;

/// <summary>A permanent log identity already exists with different content.</summary>
internal sealed class FenceLogConflict() : Exception("The fence log contains conflicting content.");

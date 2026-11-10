// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.DPoP;

/// <summary>Request-scoped binding installed only after the shared replay reservation succeeds.</summary>
internal sealed class DPoPBinding
{
    /// <summary>Gets or sets the verified key thumbprint.</summary>
    internal string? Jkt { get; set; }
}

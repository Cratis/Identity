// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Contracts.Clients;

/// <summary>The consent choices approved by the v1.1 amendment.</summary>
public enum ClientConsentType
{
    /// <summary>No consent choice was recorded; conveys no permission.</summary>
    None = 0,

    /// <summary>Explicit consent is required.</summary>
    Explicit = 1
}

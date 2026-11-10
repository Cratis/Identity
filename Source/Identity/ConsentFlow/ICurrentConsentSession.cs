// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.ConsentFlow;

/// <summary>Resolves the authenticated browser session, never a submitted request value.</summary>
internal interface ICurrentConsentSession
{
    /// <summary>Gets the session identity, or null when no trustworthy session is available.</summary>
    string? SessionId { get; }
}

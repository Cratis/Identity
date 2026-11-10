// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Sessions;

/// <summary>Limits the age of an ID token accepted as an end-session hint.</summary>
internal class EndSessionOptions
{
    /// <summary>Gets or sets the maximum age since issuance, including expired ID tokens.</summary>
    public TimeSpan EndSessionHintMaxAge { get; set; } = TimeSpan.FromHours(24);
}

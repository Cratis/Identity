// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Configuration;

/// <summary>Limits the age of an ID token accepted as an end-session hint.</summary>
public class EndSessionOptions
{
    /// <summary>Gets or sets the positive maximum age since issuance, including expired ID tokens. Defaults to one day.</summary>
    public TimeSpan EndSessionHintMaxAge { get; set; } = TimeSpan.FromDays(1);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Hosting;

/// <summary>
/// The exception that is thrown when the local-development profile is enabled but its configuration or its key
/// store is not safe to use. The profile fails closed rather than fall back to other keys.
/// </summary>
/// <param name="reason">Why the profile was refused. Never contains key material.</param>
internal class DevelopmentProfileRefused(string reason)
    : Exception($"The local-development profile (Identity:Development) was refused: {reason}");

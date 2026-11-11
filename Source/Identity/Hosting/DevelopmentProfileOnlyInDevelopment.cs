// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Hosting;

/// <summary>
/// The exception that is thrown when the local-development profile is enabled outside Development. The profile issues
/// tokens from keys no deployment trusts; it must never be reachable from a deployed host.
/// </summary>
/// <param name="environment">The name of the host environment.</param>
internal class DevelopmentProfileOnlyInDevelopment(string environment)
    : Exception($"Identity:Development:Enabled is set, but the host environment is \"{environment}\". The local-development profile runs only in the Development environment; remove the setting from this configuration (it belongs only in appsettings.Development.json).");

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Clients.Registration;

/// <summary>The result of a guarded client append.</summary>
internal enum ClientRegistrationOutcome
{
    /// <summary>The guarded append committed.</summary>
    Success = 0,

    /// <summary>Another stream already reserves the OAuth identifier.</summary>
    AlreadyRegistered = 1,

    /// <summary>The stream changed since the guarded read.</summary>
    Conflict = 2
}

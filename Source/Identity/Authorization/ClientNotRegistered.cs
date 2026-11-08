// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Authorization;

/// <summary>
/// The exception that is thrown when a client OpenIddict already validated can no longer be found in the store.
/// </summary>
/// <param name="clientId">The client identifier.</param>
internal class ClientNotRegistered(string clientId)
    : Exception($"The client \"{clientId}\" is not registered.");

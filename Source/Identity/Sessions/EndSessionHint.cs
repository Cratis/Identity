// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Sessions;

/// <summary>A verified ID-token hint and its registered relying party.</summary>
/// <param name="ClientId">The single relying-party audience.</param>
/// <param name="Subject">The signed-in account subject.</param>
/// <param name="SessionId">The browser-session identifier, when present.</param>
/// <param name="Application">The registered OpenIddict application.</param>
internal record EndSessionHint(string ClientId, string Subject, string? SessionId, object Application);

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Authorization;

/// <summary>Describes an identity-only consent page without a resource or tenant selector.</summary>
/// <param name="ClientName">The registered client's display name.</param>
/// <param name="RedirectUri">The registered callback.</param>
/// <param name="Grant">The requested identity grant.</param>
/// <param name="Parameters">The authorization parameters preserved through consent.</param>
internal sealed record IdentityConsent(string ClientName, Uri RedirectUri, IdentityOnlyGrant Grant, IEnumerable<KeyValuePair<string, string>> Parameters);

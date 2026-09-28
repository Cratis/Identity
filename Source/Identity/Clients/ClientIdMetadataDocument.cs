// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Clients;

/// <summary>
/// Represents the parts of an OAuth Client ID Metadata Document (CIMD) Identity would act on.
/// </summary>
/// <param name="ClientId">The client identifier, which is the HTTPS URL the document was fetched from.</param>
/// <param name="ClientName">The self-asserted client name. Untrusted presentation metadata.</param>
/// <param name="RedirectUris">The redirect URIs the document declares.</param>
public record ClientIdMetadataDocument(Uri ClientId, string ClientName, IEnumerable<Uri> RedirectUris);

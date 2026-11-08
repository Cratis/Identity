// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Authorization;

/// <summary>
/// The exception that is thrown when an OpenIddict passthrough endpoint is reached without an OpenIddict request,
/// which means the endpoint was mapped outside the OpenIddict pipeline.
/// </summary>
/// <param name="path">The request path.</param>
internal class OpenIddictRequestMissing(string path)
    : Exception($"The OpenIddict request for \"{path}\" could not be retrieved; the endpoint must be served through OpenIddict.");

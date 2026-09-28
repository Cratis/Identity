// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Hosting;

/// <summary>
/// The exception that is thrown when Identity starts outside development without an explicit issuer. Deriving
/// the issuer from the request host would let a spoofed host header change the <c language="text">iss</c> of issued tokens.
/// </summary>
public class IssuerNotConfigured()
    : Exception("Identity:Issuer must be configured outside development.");

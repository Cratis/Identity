// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Hosting;

/// <summary>
/// The exception that is thrown when Identity starts outside development without signing and encryption
/// certificates. Ephemeral keys would invalidate every issued token on restart and differ between replicas.
/// </summary>
public class CertificatesNotConfigured()
    : Exception("Identity:SigningCertificate and Identity:EncryptionCertificate must be configured outside development.");

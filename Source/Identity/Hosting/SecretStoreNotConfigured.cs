// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Hosting;

/// <summary>
/// The exception that is thrown when Identity starts outside development without a secret store. Ephemeral keys
/// would invalidate every issued token on restart and differ between replicas.
/// </summary>
public class SecretStoreNotConfigured()
    : Exception("Identity:SecretStore:Path must be configured outside development. It is a directory holding signing.<name>.crt/.key, encryption.<name>.crt/.key and data-protection.<name>.xml.");

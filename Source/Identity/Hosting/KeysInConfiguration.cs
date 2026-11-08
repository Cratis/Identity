// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Hosting;

/// <summary>
/// The exception that is thrown when the configuration still carries a signing or encryption certificate. Keys
/// live in the secret store only; silently ignoring a stale setting would leave the operator believing a key is in use.
/// </summary>
/// <param name="setting">The configuration key that was found.</param>
internal class KeysInConfiguration(string setting)
    : Exception($"{setting} is no longer supported. Keys are read from the secret store directory Identity:SecretStore:Path (signing.<name>.crt/.key, encryption.<name>.crt/.key, data-protection.<name>.xml).");

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Keys;

/// <summary>
/// Holds log messages for <see cref="SecretStoreKeys"/>. Only file names and fixed reasons are logged, never key bytes.
/// </summary>
internal static partial class SecretStoreKeysLogging
{
    [LoggerMessage(LogLevel.Error, "The secret store was not reloaded and the previous keys stay in use: {File}: {Reason}")]
    internal static partial void ReloadRefused(this ILogger<SecretStoreKeys> logger, string file, string reason);

    [LoggerMessage(LogLevel.Critical, "Token issuance is stopped until a key is provisioned in the secret store: {Reason}")]
    internal static partial void IssuanceStopped(this ILogger<SecretStoreKeys> logger, string reason);

    [LoggerMessage(LogLevel.Information, "Realm keys changed: signing with {Signer}, encrypting with {Encrypter}")]
    internal static partial void KeysChanged(this ILogger<SecretStoreKeys> logger, string signer, string encrypter);

    [LoggerMessage(LogLevel.Warning, "The Data Protection key in use expires at {Expires}; provision a successor in the secret store")]
    internal static partial void DataProtectionKeyExpiring(this ILogger<SecretStoreKeys> logger, DateTimeOffset expires);
}

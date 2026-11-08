// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.DataProtection.KeyManagement;

namespace Cratis.Identity.Keys;

/// <summary>
/// Wraps the Data Protection key manager so that a reload of the secret store invalidates the cached key ring.
/// </summary>
/// <remarks>
/// ASP.NET Core caches the key ring until the cache expiration token of the key manager fires, up to a day. The
/// inner manager reads the repository on every call, so firing the token on a reload is enough for a newly added
/// key to protect, and a removed key to stop being the default, on the next operation.
/// </remarks>
/// <param name="inner">The wrapped <see cref="IKeyManager"/>.</param>
/// <param name="keys">The <see cref="SecretStoreKeys"/>.</param>
internal class SecretStoreKeyManager(IKeyManager inner, SecretStoreKeys keys) : IKeyManager
{
    /// <inheritdoc/>
    public IKey CreateNewKey(DateTimeOffset activationDate, DateTimeOffset expirationDate) => inner.CreateNewKey(activationDate, expirationDate);

    /// <inheritdoc/>
    public IReadOnlyCollection<IKey> GetAllKeys() => inner.GetAllKeys();

    /// <inheritdoc/>
    public CancellationToken GetCacheExpirationToken() => keys.CacheExpirationToken;

    /// <inheritdoc/>
    public void RevokeKey(Guid keyId, string? reason = null) => inner.RevokeKey(keyId, reason);

    /// <inheritdoc/>
    public void RevokeAllKeys(DateTimeOffset revocationDate, string? reason = null) => inner.RevokeAllKeys(revocationDate, reason);
}

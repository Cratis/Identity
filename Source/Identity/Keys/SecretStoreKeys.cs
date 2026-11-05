// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Microsoft.Extensions.Primitives;

namespace Cratis.Identity.Keys;

/// <summary>
/// Holds the current <see cref="KeySnapshot"/> of the secret store and reads it again on demand.
/// </summary>
/// <remarks>
/// The first read, through <see cref="Current"/>, throws when the store is not valid; the host reads it when the service
/// is registered for use, so Identity fails closed at startup. A later read that finds invalid material keeps the last valid snapshot and logs the file and reason.
/// </remarks>
/// <param name="options">The <see cref="SecretStoreOptions"/>.</param>
/// <param name="timeProvider">The <see cref="TimeProvider"/>.</param>
/// <param name="logger">The <see cref="ILogger"/>.</param>
public sealed class SecretStoreKeys(SecretStoreOptions options, TimeProvider timeProvider, ILogger<SecretStoreKeys> logger) : IDisposable
{
    static readonly TimeSpan _dataProtectionExpiryWarning = TimeSpan.FromDays(14);
    readonly Lock _lock = new();
    readonly KeyEligibility _eligibility = new(timeProvider);
    CancellationTokenSource _changed = new();
    KeySnapshot _current = default!;

    /// <summary>
    /// Gets the current <see cref="KeySnapshot"/>.
    /// </summary>
    public KeySnapshot Current
    {
        get
        {
            // The first read doubles as startup validation.
            lock (_lock)
            {
                return _current ??= Build();
            }
        }
    }

    /// <summary>
    /// Gets a token that fires when the published keys, or the keys in use, change. A new token is handed out after every fire.
    /// </summary>
    public IChangeToken ChangeToken
    {
        get
        {
            lock (_lock)
            {
                return new CancellationChangeToken(_changed.Token);
            }
        }
    }

    /// <summary>
    /// Gets the next moment at which the keys in use can change on their own, as time passes.
    /// </summary>
    public DateTimeOffset? NextBoundary => _eligibility.NextBoundary(Current.Ring.Signing.Concat(Current.Ring.Encryption));

    /// <summary>
    /// Reads the secret store again. Invalid material keeps the previous snapshot.
    /// </summary>
    public void Reload()
    {
        lock (_lock)
        {
            _current ??= Build();
            KeySnapshot next;
            try
            {
                next = Build();
            }
            catch (InvalidKeyMaterial exception)
            {
                logger.ReloadRefused(exception.File, exception.Reason);
                return;
            }
            catch (Exception exception) when (exception is NoUsableKey or IOException or UnauthorizedAccessException)
            {
                logger.ReloadRefused(options.Path, exception.GetType().Name);
                return;
            }

            if (!next.DiffersFrom(_current))
            {
                _current = next;
                return;
            }

            _current = next;
            var previous = _changed;
            _changed = new CancellationTokenSource();
            logger.KeysChanged(next.Signer.Kid, next.Encrypter.Kid);
            previous.Cancel();
            previous.Dispose();
        }
    }

    /// <inheritdoc/>
    public void Dispose() => _changed.Dispose();

    KeySnapshot Build()
    {
        var ring = KeyRingLoader.Load(SecretStoreDirectory.Read(options.Path));
        var now = timeProvider.GetUtcNow();

        var active = ring.DataProtection.Where(_ => _.ActivationDate <= now && now < _.ExpirationDate).ToList();
        if (active.Count == 0)
        {
            throw new InvalidKeyMaterial("data-protection.*.xml", "no key in the Data Protection ring is active now");
        }

        var defaultKey = active.MaxBy(_ => _.ActivationDate)!;
        if (defaultKey.ExpirationDate - now < _dataProtectionExpiryWarning)
        {
            logger.DataProtectionKeyExpiring(defaultKey.ExpirationDate);
        }

        return new KeySnapshot(
            ring,
            _eligibility.Current(KeyPurpose.Signing, ring.Signing),
            _eligibility.Current(KeyPurpose.Encryption, ring.Encryption));
    }
}

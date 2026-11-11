// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Cratis.Identity.Development;
using Cratis.Identity.Hosting;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;

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
/// <param name="providesDataProtection">Whether the store also holds the Data Protection ring. The local-development profile keeps that ring in a Data Protection key directory instead.</param>
/// <param name="developmentRoot">The pinned custody of local-development keys, or null for a deployed store.</param>
/// <param name="observeOidc">Whether configured OIDC clients require readiness-transition diagnostics.</param>
internal sealed class SecretStoreKeys(SecretStoreOptions options, TimeProvider timeProvider, ILogger<SecretStoreKeys> logger, bool providesDataProtection = true, DevelopmentKeyStoreRoot? developmentRoot = null, bool observeOidc = false) : IDisposable
{
    static readonly TimeSpan _dataProtectionExpiryWarning = TimeSpan.FromDays(14);
    readonly Lock _lock = new();
    readonly KeyEligibility _eligibility = new(timeProvider);
    CancellationTokenSource _changed = new();
    KeySnapshot _current = default!;
    bool _keysUnusable;
    bool _lastReadSucceeded = true;
    bool? _oidcReady;

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
                return _current ??= InitialSnapshot();
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether tokens may be issued: the last read found usable keys, and the signing and
    /// encryption keys in use have not expired since.
    /// </summary>
    public bool CanIssue
    {
        get
        {
            var snapshot = Current;
            var now = timeProvider.GetUtcNow();
            lock (_lock)
            {
                return !_keysUnusable && now < snapshot.Signer.NotAfter && now < snapshot.Encrypter.NotAfter;
            }
        }
    }

    /// <summary>
    /// Gets the live OIDC key-readiness signal, without changing OAuth's last-valid-snapshot behavior.
    /// </summary>
    public bool CanIssueIdentityTokens
    {
        get
        {
            lock (_lock)
            {
                return _lastReadSucceeded && !_keysUnusable && OidcSigningKey.Select(Current.Ring.Signing, timeProvider.GetUtcNow()) is not null;
            }
        }
    }

    /// <summary>
    /// Resolves readiness and the eligible ID-token credential against one ring snapshot.
    /// </summary>
    /// <param name="credentials">The server's registered signing credentials.</param>
    /// <param name="now">The issuance time.</param>
    /// <returns>The credential admitted by the current ring, or null when issuance is unavailable.</returns>
    internal SigningCredentials? FindIdentityTokenCredential(IEnumerable<SigningCredentials> credentials, DateTimeOffset now)
    {
        lock (_lock)
        {
            var snapshot = Current;
            if (!_lastReadSucceeded || _keysUnusable) return null;
            if (now >= snapshot.Signer.NotAfter || now >= snapshot.Encrypter.NotAfter) return null;

            var selected = OidcSigningKey.Select(snapshot.Ring.Signing, now);
            return selected is null ? null : credentials.FirstOrDefault(credential =>
                credential.Algorithm == SecurityAlgorithms.RsaSha256 && credential.Kid == selected.Kid);
        }
    }

    /// <summary>
    /// Gets a cancellation token that fires with <see cref="ChangeToken"/>, for the Data Protection key ring cache.
    /// </summary>
    public CancellationToken CacheExpirationToken
    {
        get
        {
            lock (_lock)
            {
                return _changed.Token;
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
                _lastReadSucceeded = false;
                ReportOidcReadiness(false);
                logger.ReloadRefused(exception.File, exception.Reason);
                return;
            }
            catch (NoUsableKey exception)
            {
                // No unexpired key: fail closed rather than keep signing with an expired certificate.
                _keysUnusable = true;
                _lastReadSucceeded = false;
                ReportOidcReadiness(false);
                logger.IssuanceStopped(exception.Message);
                return;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or DevelopmentProfileRefused)
            {
                _lastReadSucceeded = false;
                ReportOidcReadiness(false);
                logger.ReloadRefused(options.Path, exception.GetType().Name);
                return;
            }

            _keysUnusable = false;
            _lastReadSucceeded = true;
            ReportOidcReadiness(OidcSigningKey.Select(next.Ring.Signing, timeProvider.GetUtcNow()) is not null);
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

    KeySnapshot InitialSnapshot()
    {
        var snapshot = Build();
        _oidcReady = OidcSigningKey.Select(snapshot.Ring.Signing, timeProvider.GetUtcNow()) is not null;
        return snapshot;
    }

    void ReportOidcReadiness(bool ready)
    {
        if (!observeOidc || _oidcReady == ready) return;
        _oidcReady = ready;
        if (ready) logger.OidcSigningRestored();
        else logger.OidcSigningUnavailable();
    }

    KeySnapshot Build()
    {
        var ring = KeyRingLoader.Load(developmentRoot is null ? SecretStoreDirectory.Read(options.Path) : developmentRoot.ReadKeyFiles());
        var now = timeProvider.GetUtcNow();

        var active = ring.DataProtection.Where(_ => _.ActivationDate <= now && now < _.ExpirationDate).ToList();
        if (!providesDataProtection)
        {
            return Snapshot(ring);
        }

        if (active.Count == 0)
        {
            throw new InvalidKeyMaterial("data-protection.*.xml", "no key in the Data Protection ring is active now");
        }

        var defaultKey = active.MaxBy(_ => _.ActivationDate)!;
        if (defaultKey.ExpirationDate - now < _dataProtectionExpiryWarning)
        {
            logger.DataProtectionKeyExpiring(defaultKey.ExpirationDate);
        }

        return Snapshot(ring);
    }

    KeySnapshot Snapshot(KeyRing ring) => new(
        ring,
        _eligibility.Current(KeyPurpose.Signing, ring.Signing),
        _eligibility.Current(KeyPurpose.Encryption, ring.Encryption));
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Keys;

/// <summary>
/// Decides which key signs or encrypts now, so a new key is published everywhere before any replica uses it.
/// </summary>
/// <remarks>
/// A key is eligible once it has been valid for <see cref="PrePublication"/>, which covers a resource server's
/// JWKS cache. The current key is the eligible one with the latest start of validity. When nothing is eligible yet
/// (a first deployment) the valid key that started first is used, because pre-publication only matters while a
/// predecessor exists. Every key stays published, and usable for decryption, until the operator removes it.
/// </remarks>
/// <param name="timeProvider">The <see cref="TimeProvider"/>.</param>
public class KeyEligibility(TimeProvider timeProvider)
{
    /// <summary>
    /// Gets how long a key is valid before it becomes the one that signs or encrypts.
    /// </summary>
    public static readonly TimeSpan PrePublication = TimeSpan.FromSeconds(600);

    /// <summary>
    /// Gets the key that signs or encrypts now.
    /// </summary>
    /// <param name="purpose">The <see cref="KeyPurpose"/> of <paramref name="keys"/>.</param>
    /// <param name="keys">The keys of that purpose.</param>
    /// <returns>The current <see cref="RealmKey"/>.</returns>
    /// <exception cref="NoUsableKey">Thrown when no key is valid now.</exception>
    public RealmKey Current(KeyPurpose purpose, IReadOnlyList<RealmKey> keys)
    {
        var now = timeProvider.GetUtcNow();
        var eligible = keys.Where(_ => _.NotBefore + PrePublication <= now && now < _.NotAfter).ToList();
        if (eligible.Count > 0)
        {
            return eligible.OrderByDescending(_ => _.NotBefore).ThenBy(_ => _.Kid, StringComparer.Ordinal).First();
        }

        return keys
            .Where(_ => _.NotBefore <= now && now < _.NotAfter)
            .OrderBy(_ => _.NotBefore)
            .ThenBy(_ => _.Kid, StringComparer.Ordinal)
            .FirstOrDefault() ?? throw new NoUsableKey(purpose);
    }

    /// <summary>
    /// Gets the next moment at which the answer of <see cref="Current"/> can change.
    /// </summary>
    /// <param name="keys">The keys of all purposes.</param>
    /// <returns>The next boundary, or null when there is none.</returns>
    public DateTimeOffset? NextBoundary(IEnumerable<RealmKey> keys)
    {
        var now = timeProvider.GetUtcNow();
        var boundaries = keys
            .SelectMany(_ => new[] { _.NotBefore, _.NotBefore + PrePublication, _.NotAfter })
            .Where(_ => _ > now)
            .ToList();

        return boundaries.Count == 0 ? null : boundaries.Min();
    }
}

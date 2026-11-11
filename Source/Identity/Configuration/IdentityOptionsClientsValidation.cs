// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Clients.MetadataDocuments;

namespace Cratis.Identity.Configuration;

/// <summary>Hard metadata resource bounds, independent of environment and enablement.</summary>
internal static class IdentityOptionsClientsValidation
{
    const string Prefix = "Identity:Clients:MetadataDocuments:";

    /// <summary>Validates metadata limits without including untrusted client values.</summary>
    /// <param name="options">The issuer policy.</param>
    /// <returns>Configuration failures naming the relevant keys.</returns>
    public static IEnumerable<string> Validate(ClientIdMetadataOptions options) => ValidateTransport(options).Concat(ValidateCache(options)).Concat(ValidateAdmission(options));

    static IEnumerable<string> ValidateTransport(ClientIdMetadataOptions options)
    {
        if (options.MaxDocumentBytes is < 1024 or > 65_536) yield return Prefix + "MaxDocumentBytes must be between 1024 and 65536.";
        if (options.RequestTimeout <= TimeSpan.Zero || options.RequestTimeout >= TimeSpan.FromSeconds(10)) yield return Prefix + "RequestTimeout must be positive and less than ten seconds.";
        if (options.ConnectTimeout <= TimeSpan.Zero || options.ConnectTimeout > options.RequestTimeout) yield return Prefix + "ConnectTimeout must be positive and no greater than RequestTimeout.";
        if (options.MaxRedirectUris is < 1 or > 10) yield return Prefix + "MaxRedirectUris must be between 1 and 10.";
    }

    static IEnumerable<string> ValidateCache(ClientIdMetadataOptions options)
    {
        if (options.MaxCacheTtl <= TimeSpan.Zero || options.MaxCacheTtl > TimeSpan.FromHours(24)) yield return Prefix + "MaxCacheTtl must be positive and at most 24 hours.";
        if (options.MinCacheTtl <= TimeSpan.Zero || options.MinCacheTtl > options.DefaultCacheTtl || options.DefaultCacheTtl > options.MaxCacheTtl) yield return Prefix + "MinCacheTtl, DefaultCacheTtl and MaxCacheTtl must be positive and ordered.";
    }

    static IEnumerable<string> ValidateAdmission(ClientIdMetadataOptions options)
    {
        if (options.MaxCacheEntries is < 1 or > 4096) yield return Prefix + "MaxCacheEntries must be between 1 and 4096.";
        if (options.MaxConcurrentFetches is < 1 or > 32 || options.MaxConcurrentFetches > options.MaxCacheEntries) yield return Prefix + "MaxConcurrentFetches must be between 1 and 32 and no greater than MaxCacheEntries.";
        if (options.MaxFetchesPerMinute is < 1 or > 10000) yield return Prefix + "MaxFetchesPerMinute must be between 1 and 10000.";
        if (options.MaxFetchesPerHostPerMinute < 1 || options.MaxFetchesPerHostPerMinute > options.MaxFetchesPerMinute) yield return Prefix + "MaxFetchesPerHostPerMinute must be positive and no greater than MaxFetchesPerMinute.";
        if (options.AllowedHosts.Any(host => !ClientIdUrlPolicy.IsValid("https://" + host + "/client.json"))) yield return Prefix + "AllowedHosts must contain only canonical public DNS hostnames.";
    }
}

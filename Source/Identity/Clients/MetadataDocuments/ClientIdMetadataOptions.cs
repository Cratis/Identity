// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Clients.MetadataDocuments;

/// <summary>Issuer-owned opt-in policy for the public-client subset of CIMD draft -03.</summary>
public class ClientIdMetadataOptions
{
    /// <summary>Gets or sets whether metadata lookup is enabled. It never enables dynamic registration.</summary>
    public bool Enabled { get; set; }

    /// <summary>Gets or sets the document bound in bytes (1 KiB to 64 KiB).</summary>
    public int MaxDocumentBytes { get; set; } = 5120;

    /// <summary>Gets or sets the DNS and connection deadline.</summary>
    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(3);

    /// <summary>Gets or sets the total deadline including body reads, strictly less than ten seconds.</summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Gets or sets the maximum successful cache lifetime.</summary>
    public TimeSpan MaxCacheTtl { get; set; } = TimeSpan.FromHours(24);

    /// <summary>Gets or sets the minimum cache lifetime for responses with reusable freshness.</summary>
    public TimeSpan MinCacheTtl { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>Gets or sets the lifetime when the origin supplies no freshness directive.</summary>
    public TimeSpan DefaultCacheTtl { get; set; } = TimeSpan.FromHours(1);

    /// <summary>Gets or sets the maximum number of declared redirects.</summary>
    public int MaxRedirectUris { get; set; } = 10;

    /// <summary>Gets or sets the positive and in-flight entry bound.</summary>
    public int MaxCacheEntries { get; set; } = 256;

    /// <summary>Gets or sets the maximum simultaneous outbound fetches; no unbounded queue is created.</summary>
    public int MaxConcurrentFetches { get; set; } = 8;

    /// <summary>Gets or sets global fetch admission per minute, separate from the metadata cache.</summary>
    public int MaxFetchesPerMinute { get; set; } = 60;

    /// <summary>Gets or sets per-host fetch admission per minute.</summary>
    public int MaxFetchesPerHostPerMinute { get; set; } = 10;

    /// <summary>Gets or sets the optional exact lowercase ASCII hostname allowlist. Empty permits any public host.</summary>
    public ICollection<string> AllowedHosts { get; set; } = [];

    /// <summary>Gets or sets whether literal localhost redirects are permitted for native public CIMD clients. Pending spec amendment (woksin).</summary>
    public bool AllowLocalhostLoopback { get; set; }
}

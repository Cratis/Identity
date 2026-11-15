// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http.Headers;
using Cratis.Identity.Configuration;

namespace Cratis.Identity.Clients.MetadataDocuments;

/// <summary>Fetches only bounded, non-redirected JSON under a total request deadline.</summary>
/// <param name="http">The credential-free guarded named client.</param>
/// <param name="parser">The strict metadata parser.</param>
/// <param name="options">Issuer-owned limits.</param>
/// <param name="time">The clock for deadlines and response freshness.</param>
internal sealed class ClientIdMetadataDocumentFetcher(HttpClient http, MetadataDocumentParser parser, ClientIdMetadataOptions options, TimeProvider? time = null) : IMetadataDocumentFetcher
{
    readonly TimeProvider _time = time ?? TimeProvider.System;

    /// <inheritdoc/>
    public async ValueTask<MetadataDocumentResult> FetchAsync(ClientId clientId, CancellationToken cancellationToken)
    {
        if (!ClientIdUrlPolicy.IsValid(clientId.Value)) return MetadataDocumentResult.Refused(MetadataRefusal.InvalidUrl);
        using var timeout = new CancellationTokenSource(options.RequestTimeout, _time);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, clientId.Value);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.UserAgent.ParseAdd("Cratis-Identity");
            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
            return await ReadResponse(clientId, response, deadline.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return MetadataDocumentResult.Refused(MetadataRefusal.Timeout);
        }
        catch (HttpRequestException)
        {
            return MetadataDocumentResult.Refused(MetadataRefusal.Transport);
        }
        catch (IOException)
        {
            return MetadataDocumentResult.Refused(MetadataRefusal.Transport);
        }
    }

    static bool ValidCharset(string? charset) => charset is null || string.Equals(charset.Trim('"'), "utf-8", StringComparison.OrdinalIgnoreCase);

    static bool IsJson(HttpContentHeaders headers) => string.Equals(headers.ContentType?.MediaType, "application/json", StringComparison.OrdinalIgnoreCase) && headers.ContentEncoding.Count == 0 && ValidCharset(headers.ContentType?.CharSet);

    static bool PreventsReuse(HttpResponseHeaders headers) => headers.CacheControl?.NoStore == true || headers.CacheControl?.NoCache == true ||
        (headers.Contains("Cache-Control") && headers.CacheControl is null);

    MetadataRefusal ValidateHeaders(HttpResponseMessage response)
    {
        if (response.StatusCode != HttpStatusCode.OK) return MetadataRefusal.Status;
        if (!IsJson(response.Content.Headers)) return MetadataRefusal.ContentType;
        return response.Content.Headers.ContentLength > options.MaxDocumentBytes ? MetadataRefusal.Size : MetadataRefusal.None;
    }

    async Task<MetadataDocumentResult> ReadResponse(ClientId clientId, HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var refusal = ValidateHeaders(response);
        if (refusal != MetadataRefusal.None) return MetadataDocumentResult.Refused(refusal);
        var bytes = await ReadBounded(response.Content, cancellationToken);
        if (bytes is null) return MetadataDocumentResult.Refused(MetadataRefusal.Size);
        var result = parser.Parse(clientId.Value, bytes);
        return result with { Lifetime = Freshness(response) };
    }

    async Task<byte[]?> ReadBounded(HttpContent content, CancellationToken cancellationToken)
    {
        await using var stream = await content.ReadAsStreamAsync(cancellationToken);
        await using var buffer = new MemoryStream();
        var chunk = new byte[Math.Min(4096, options.MaxDocumentBytes + 1)];
        while (true)
        {
            var count = await stream.ReadAsync(chunk.AsMemory(0, Math.Min(chunk.Length, options.MaxDocumentBytes + 1 - (int)buffer.Length)), cancellationToken);
            if (count == 0) return buffer.ToArray();
            if (buffer.Length + count > options.MaxDocumentBytes) return null;
            await buffer.WriteAsync(chunk.AsMemory(0, count), cancellationToken);
        }
    }

    TimeSpan Freshness(HttpResponseMessage response)
    {
        if (PreventsReuse(response.Headers)) return TimeSpan.Zero;
        var lifetime = OriginLifetime(response) - (response.Headers.Age ?? TimeSpan.Zero);
        if (lifetime <= TimeSpan.Zero) return TimeSpan.Zero;
        return ClampLifetime(lifetime);
    }

    TimeSpan ClampLifetime(TimeSpan lifetime) =>
        lifetime < options.MinCacheTtl ? options.MinCacheTtl : lifetime < options.MaxCacheTtl ? lifetime : options.MaxCacheTtl;

    TimeSpan OriginLifetime(HttpResponseMessage response)
    {
        if (response.Headers.CacheControl?.MaxAge is { } maxAge) return maxAge;
        return response.Content.Headers.Expires is { } expires ? expires - (response.Headers.Date ?? _time.GetUtcNow()) : options.DefaultCacheTtl;
    }
}

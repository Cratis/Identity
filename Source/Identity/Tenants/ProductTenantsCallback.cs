// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Cratis.Identity.Accounts;
using Cratis.Identity.Configuration;
using Cratis.Identity.Resources;
using Cratis.Identity.Upstream;

namespace Cratis.Identity.Tenants;

/// <summary>
/// Obtains selectable tenants from the product without caching and fails closed on an unavailable answer.
/// </summary>
/// <param name="clients">The HTTP client factory.</param>
/// <param name="logger">The logger for fixed failure categories only.</param>
internal class ProductTenantsCallback(IHttpClientFactory clients, ILogger<ProductTenantsCallback> logger)
{
    const int MaximumBodyBytes = 64 * 1024;

    /// <summary>
    /// Looks up the person's selectable tenants under a bounded callback deadline.
    /// </summary>
    /// <param name="account">The signed-in account.</param>
    /// <param name="resource">The requested resource.</param>
    /// <param name="options">The callback configuration.</param>
    /// <param name="cancellationToken">The caller's cancellation token, whose cancellation propagates.</param>
    /// <returns>The listed tenants or a fixed unavailable category.</returns>
    internal async Task<TenantLookup> SelectableTenants(Account account, ResourceServer resource, TenantsCallbackOptions options, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (account.LinkFor(GitHubConnection.Issuer) is not { } gitHub)
        {
            return new TenantsListed([]);
        }

        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(options.Timeout);
        try
        {
            using var client = clients.CreateClient(TenantsCallbackHttpClient.Name);
            using var request = new HttpRequestMessage(HttpMethod.Post, options.Url)
            {
                Content = JsonContent.Create(new TenantsCallbackRequest(resource.Audience, account.Id.Value, gitHub.IdpSub.Value, gitHub.IdpIssuer.Value, gitHub.IdpSub.Value))
            };
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            if (!string.IsNullOrEmpty(options.Secret))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.Secret);
            }

            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
            return await ReadResponse(response, account, resource, deadline.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Unavailable(resource, account, TenantsUnavailableReason.TimedOut);
        }
        catch (HttpRequestException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Unavailable(resource, account, TenantsUnavailableReason.Unreachable);
        }
        catch (IOException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Unavailable(resource, account, TenantsUnavailableReason.Unreachable);
        }
        catch (JsonException)
        {
            return Unavailable(resource, account, TenantsUnavailableReason.Malformed);
        }
    }

    static async Task<ReadOnlyMemory<byte>> ReadBody(Stream stream, CancellationToken cancellationToken)
    {
        var bytes = new byte[MaximumBodyBytes + 1];
        var length = 0;
        while (length < bytes.Length)
        {
            var read = await stream.ReadAsync(bytes.AsMemory(length), cancellationToken);
            if (read == 0)
            {
                break;
            }

            length += read;
        }

        return bytes.AsMemory(0, length);
    }

    static bool TryGetSubject(JsonElement body, out JsonElement subject)
    {
        subject = default;
        return body.ValueKind == JsonValueKind.Object && body.TryGetProperty("subject", out subject) &&
            subject.ValueKind == JsonValueKind.String;
    }

    async Task<TenantLookup> ReadResponse(HttpResponseMessage response, Account account, ResourceServer resource, CancellationToken cancellationToken)
    {
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var reason = response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
                ? TenantsUnavailableReason.Refused
                : TenantsUnavailableReason.Unreachable;
            return Unavailable(resource, account, reason);
        }

        if (!string.Equals(response.Content.Headers.ContentType?.MediaType, "application/json", StringComparison.OrdinalIgnoreCase) ||
            response.Content.Headers.ContentLength > MaximumBodyBytes)
        {
            return Unavailable(resource, account, TenantsUnavailableReason.Malformed);
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var bytes = await ReadBody(stream, cancellationToken);
        if (bytes.Length > MaximumBodyBytes)
        {
            return Unavailable(resource, account, TenantsUnavailableReason.Malformed);
        }

        using var document = JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = 8 });
        return ReadTenants(document.RootElement, account, resource, cancellationToken);
    }

    TenantLookup ReadTenants(JsonElement body, Account account, ResourceServer resource, CancellationToken cancellationToken)
    {
        IEnumerable<SelectableTenant>? tenants;
        try
        {
            if (!TryGetSubject(body, out var subject))
            {
                return Unavailable(resource, account, TenantsUnavailableReason.Malformed);
            }

            var subjectId = subject.GetString();
            if (!string.Equals(subjectId, account.Id, StringComparison.Ordinal))
            {
                return Unavailable(resource, account, TenantsUnavailableReason.WrongSubject);
            }

            tenants = TenantsCallbackResponse.Read(body);
        }
        catch (InvalidOperationException)
        {
            // JSON strings containing unpaired surrogates cannot be decoded.
            return Unavailable(resource, account, TenantsUnavailableReason.Malformed);
        }

        cancellationToken.ThrowIfCancellationRequested();
        return tenants is null ? Unavailable(resource, account, TenantsUnavailableReason.Malformed) : new TenantsListed(tenants);
    }

    TenantsUnavailable Unavailable(ResourceServer resource, Account account, TenantsUnavailableReason reason)
    {
        logger.Unavailable(resource.Audience, account.Id, reason);
        return new TenantsUnavailable(reason);
    }
}

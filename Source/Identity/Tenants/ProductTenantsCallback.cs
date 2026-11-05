// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Cratis.Identity.Accounts;
using Cratis.Identity.Configuration;
using Cratis.Identity.Resources;

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
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(options.Timeout);
        try
        {
            using var client = clients.CreateClient(TenantsCallbackHttpClient.Name);
            using var request = new HttpRequestMessage(HttpMethod.Post, options.Url)
            {
                Content = JsonContent.Create(new TenantsCallbackRequest(resource.Audience, account.Id, account.GitHubId.ToString(CultureInfo.InvariantCulture)))
            };
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            if (!string.IsNullOrEmpty(options.Secret))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.Secret);
            }

            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                return Unavailable(response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
                    ? TenantsUnavailableReason.Refused
                    : TenantsUnavailableReason.Unreachable);
            }

            if (!string.Equals(response.Content.Headers.ContentType?.MediaType, "application/json", StringComparison.OrdinalIgnoreCase) ||
                response.Content.Headers.ContentLength > MaximumBodyBytes)
            {
                return Unavailable(TenantsUnavailableReason.Malformed);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(deadline.Token);
            var bytes = new byte[MaximumBodyBytes + 1];
            var length = 0;
            while (length < bytes.Length)
            {
                var read = await stream.ReadAsync(bytes.AsMemory(length), deadline.Token);
                if (read == 0)
                {
                    break;
                }

                length += read;
            }

            if (length > MaximumBodyBytes)
            {
                return Unavailable(TenantsUnavailableReason.Malformed);
            }

            using var document = JsonDocument.Parse(bytes.AsMemory(0, length), new JsonDocumentOptions { MaxDepth = 8 });
            var body = document.RootElement;
            if (body.ValueKind == JsonValueKind.Object && body.TryGetProperty("subject", out var subject) &&
                subject.ValueKind == JsonValueKind.String && !string.Equals(subject.GetString(), account.Id, StringComparison.Ordinal))
            {
                return Unavailable(TenantsUnavailableReason.WrongSubject);
            }

            var tenants = TenantsCallbackResponse.Read(body, account.Id);
            deadline.Token.ThrowIfCancellationRequested();
            return tenants is null ? Unavailable(TenantsUnavailableReason.Malformed) : new TenantsListed(tenants);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Unavailable(TenantsUnavailableReason.TimedOut);
        }
        catch (HttpRequestException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Unavailable(TenantsUnavailableReason.Unreachable);
        }
        catch (IOException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Unavailable(TenantsUnavailableReason.Unreachable);
        }
        catch (JsonException)
        {
            return Unavailable(TenantsUnavailableReason.Malformed);
        }

        TenantsUnavailable Unavailable(TenantsUnavailableReason reason)
        {
            logger.Unavailable(resource.Audience, account.Id, reason);
            return new TenantsUnavailable(reason);
        }
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Net.Http.Json;
using Cratis.Identity.Tenants;

namespace Cratis.Identity.Integration.given;

public class TenantsCallbackStub : HttpMessageHandler
{
    readonly ConcurrentDictionary<string, SelectableTenant[]> _tenants = new(StringComparer.Ordinal);
    readonly ConcurrentDictionary<string, bool> _unavailable = new(StringComparer.Ordinal);

    public void List(string gitHubId, params SelectableTenant[] tenants)
    {
        _tenants[gitHubId] = tenants;
        _unavailable[gitHubId] = false;
    }

    public void Fail(string gitHubId) => _unavailable[gitHubId] = true;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var body = JsonDocument.Parse(await request.Content.ReadAsStringAsync(cancellationToken));
        var gitHubId = body.RootElement.GetProperty("github_id").GetString() ?? string.Empty;
        if (_unavailable.GetValueOrDefault(gitHubId))
        {
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        }

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new
            {
                subject = body.RootElement.GetProperty("subject").GetString(),
                tenants = _tenants.GetValueOrDefault(gitHubId, []).Select(_ => new { id = _.Id, name = _.Name })
            })
        };
    }
}

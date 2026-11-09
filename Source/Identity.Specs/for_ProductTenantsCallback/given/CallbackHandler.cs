// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text;

namespace Cratis.Identity.for_ProductTenantsCallback.given;

public class CallbackHandler : HttpMessageHandler
{
    public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;
    public string Body { get; set; } = /*lang=json*/ """{"subject":"7d9d5c9e-0000-4000-8000-000000000001","tenants":[{"id":"one","name":"One"},{"id":"two","name":"Two"}]}""";
    public string MediaType { get; set; } = "application/json";
    public Func<HttpContent>? Content { get; set; }
    public Func<CancellationToken, Task>? BeforeResponse { get; set; }
    public List<HttpRequestMessage> Requests { get; } = [];
    public string? RequestBody { get; set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        RequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
        if (BeforeResponse is not null)
        {
            await BeforeResponse(cancellationToken);
        }

        return new HttpResponseMessage(Status) { Content = Content?.Invoke() ?? new StringContent(Body, Encoding.UTF8, MediaType) };
    }
}

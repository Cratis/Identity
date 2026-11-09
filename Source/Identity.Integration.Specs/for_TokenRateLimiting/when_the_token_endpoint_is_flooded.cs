// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Identity.Configuration;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Identity.Integration.for_TokenRateLimiting;

[Collection(IdentityCollection.Name)]
public class when_the_token_endpoint_is_flooded(IdentityHost host) : IAsyncLifetime
{
    readonly ManualClock _time = new();
    HttpResponseMessage _first;
    HttpResponseMessage _limited;
    HttpResponseMessage _recovered;
    string _body;

    public async ValueTask InitializeAsync()
    {
        await using var factory = host.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<TimeProvider>(_time);
            services.PostConfigure<IdentityOptions>(options => options.TokenRateLimit = new() { PerSourceRequests = 1 });
        }));
        using var client = factory.CreateClient(new() { BaseAddress = new(IdentityHost.Issuer) });
        await IdentityHost.WaitForRecovery(factory.Services);
        _first = await Send(client);
        _limited = await Send(client);
        _body = await _limited.Content.ReadAsStringAsync();
        _time.Now += TimeSpan.FromMinutes(2);
        _recovered = await Send(client);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    static Task<HttpResponseMessage> Send(HttpClient client)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/connect/token") { Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["grant_type"] = "refresh_token", ["refresh_token"] = "invalid-refresh-token" }) };
        request.Headers.Authorization = new("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes("unregistered:incorrect")));
        return client.SendAsync(request);
    }

    [Fact] void should_preserve_invalid_client_before_the_limit() => _first.StatusCode.ShouldEqual(HttpStatusCode.Unauthorized);
    [Fact] void should_limit_before_client_authentication() => _limited.StatusCode.ShouldEqual(HttpStatusCode.TooManyRequests);
    [Fact] void should_use_a_generic_oauth_refusal() => JsonDocument.Parse(_body).RootElement.GetProperty("error").GetString().ShouldEqual("temporarily_unavailable");
    [Fact] void should_give_the_full_window() => _limited.Headers.RetryAfter.Delta.ShouldEqual(TimeSpan.FromMinutes(1));
    [Fact] void should_allow_protocol_validation_after_the_window() => _recovered.StatusCode.ShouldEqual(_first.StatusCode);

    sealed class ManualClock : TimeProvider
    {
        internal DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => Now;
    }
}

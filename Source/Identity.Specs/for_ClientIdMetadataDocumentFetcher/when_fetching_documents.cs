// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Cratis.Identity.Clients;
using Cratis.Identity.Clients.MetadataDocuments;
using Cratis.Identity.for_ClientIdMetadataDocumentFetcher.given;
using IdentityOptionsClients = Cratis.Identity.Clients.MetadataDocuments.ClientIdMetadataOptions;

namespace Cratis.Identity.for_ClientIdMetadataDocumentFetcher;

public class when_fetching_documents : Specification
{
    const string ClientId = "https://client.example/cimd.json";
    const string Valid = /*lang=json,strict*/ "{\"client_id\":\"https://client.example/cimd.json\",\"redirect_uris\":[\"https://client.example/callback\"]}";

    [Theory]
    [InlineData(301)]
    [InlineData(302)]
    [InlineData(307)]
    [InlineData(308)]
    [InlineData(204)]
    [InlineData(206)]
    [InlineData(404)]
    [InlineData(500)]
    public async Task should_refuse_every_status_except_200_without_following_locations(int status)
    {
        using var handler = new ResponseHandler(() => new HttpResponseMessage((HttpStatusCode)status) { Headers = { Location = new Uri("http://169.254.169.254/") } });
        (await Fetch(handler)).Descriptor.ShouldBeNull();
        handler.Requests.ShouldEqual(1);
    }

    [Theory]
    [InlineData("text/html")]
    [InlineData("application/jsonp")]
    [InlineData("application/problem+json")]
    [InlineData(null)]
    public async Task should_require_exact_json_media_type(string? mediaType)
    {
        using var handler = new ResponseHandler(() => Response(Valid, mediaType));
        (await Fetch(handler)).Descriptor.ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("[]")]
    [InlineData(/*lang=json,strict*/ "{\"client_id\":1}")]
    public async Task should_refuse_invalid_documents(string body)
    {
        using var handler = new ResponseHandler(() => Response(body));
        (await Fetch(handler)).Descriptor.ShouldBeNull();
    }

    [Fact]
    async Task should_bound_declared_and_streamed_lengths()
    {
        using var handler = new ResponseHandler(() => Response(new string('a', 1024)));
        (await Fetch(handler, new IdentityOptionsClients { MaxDocumentBytes = 512 })).Refusal.ShouldEqual(MetadataRefusal.Size);
        using var streaming = new ResponseHandler(() => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(new string('a', 1024)))) { Headers = { ContentType = new MediaTypeHeaderValue("application/json"), ContentLength = 1 } }
        });
        (await Fetch(streaming, new IdentityOptionsClients { MaxDocumentBytes = 512 })).Refusal.ShouldEqual(MetadataRefusal.Size);
    }

    [Fact]
    async Task should_cancel_a_stalled_response_at_the_total_deadline()
    {
        using var handler = new ResponseHandler(async cancellation =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellation);
            return Response(Valid);
        });
        var clock = new DeadlineClock();
        var fetch = Fetch(handler, time: clock);
        clock.Expire();
        (await fetch).Refusal.ShouldEqual(MetadataRefusal.Timeout);
    }

    [Fact]
    async Task should_apply_the_total_deadline_to_a_stalled_body()
    {
        using var handler = new ResponseHandler(() => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(new StalledStream()) { Headers = { ContentType = new MediaTypeHeaderValue("application/json") } }
        });
        var clock = new DeadlineClock();
        var fetch = Fetch(handler, time: clock);
        clock.Expire();
        (await fetch).Refusal.ShouldEqual(MetadataRefusal.Timeout);
    }

    [Fact]
    async Task should_use_the_injected_clock_when_expires_has_no_date_header()
    {
        var now = DateTimeOffset.UnixEpoch;
        using var handler = new ResponseHandler(() =>
        {
            var response = Response(Valid);
            response.Content.Headers.Expires = now + TimeSpan.FromMinutes(5);
            return response;
        });
        (await Fetch(handler, time: new Cratis.Identity.given.FixedTimeProvider(now))).Lifetime.ShouldEqual(TimeSpan.FromMinutes(5));
    }

    [Fact]
    async Task should_refuse_compressed_content_without_decompressing_it()
    {
        using var handler = new ResponseHandler(() =>
        {
            var response = Response(Valid);
            response.Content.Headers.ContentEncoding.Add("gzip");
            return response;
        });
        (await Fetch(handler)).Refusal.ShouldEqual(MetadataRefusal.ContentType);
    }

    [Theory]
    [InlineData("no-store", 0)]
    [InlineData("no-cache", 0)]
    [InlineData("max-age=0", 0)]
    [InlineData("max-age=10", 60)]
    [InlineData("max-age=172800", 86400)]
    public async Task should_obey_freshness_without_allowing_a_server_to_extend_the_cap(string cacheControl, int seconds)
    {
        using var handler = new ResponseHandler(() =>
        {
            var response = Response(Valid);
            response.Headers.CacheControl = CacheControlHeaderValue.Parse(cacheControl);
            return response;
        });
        (await Fetch(handler)).Lifetime.ShouldEqual(TimeSpan.FromSeconds(seconds));
    }

    [Fact]
    async Task should_never_send_credentials_or_request_compression()
    {
        using var handler = new ResponseHandler(() => Response(Valid));
        (await Fetch(handler)).Descriptor.ShouldNotBeNull();
        handler.SafeHeaders.ShouldBeTrue();
    }

    [Fact]
    async Task should_refuse_a_bad_url_before_http()
    {
        using var handler = new ResponseHandler(() => Response(Valid));
        using var client = new HttpClient(handler);
        var options = new IdentityOptionsClients();
        (await new ClientIdMetadataDocumentFetcher(client, new MetadataDocumentParser(options), options).FetchAsync(new ClientId("https://client.example/cimd.json?synthetic=1"), CancellationToken.None)).Descriptor.ShouldBeNull();
        handler.Requests.ShouldEqual(0);
    }

    static async Task<MetadataDocumentResult> Fetch(ResponseHandler handler, IdentityOptionsClients? options = null, TimeProvider? time = null)
    {
        options ??= new IdentityOptionsClients();
        using var client = new HttpClient(handler, disposeHandler: false);
        return await new ClientIdMetadataDocumentFetcher(client, new MetadataDocumentParser(options), options, time).FetchAsync(new ClientId(ClientId), CancellationToken.None);
    }

    static HttpResponseMessage Response(string text, string? contentType = "application/json")
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(Encoding.UTF8.GetBytes(text)) };
        response.Content.Headers.ContentType = contentType is null ? null : new MediaTypeHeaderValue(contentType);
        return response;
    }

    sealed class StalledStream : MemoryStream
    {
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => new(Stall(cancellationToken));

        static async Task<int> Stall(CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return 0;
        }
    }

    sealed class ResponseHandler : HttpMessageHandler
    {
        readonly Func<CancellationToken, Task<HttpResponseMessage>> _response;

        public ResponseHandler(Func<HttpResponseMessage> response) => _response = _ => Task.FromResult(response());
        public ResponseHandler(Func<CancellationToken, Task<HttpResponseMessage>> response) => _response = response;
        public int Requests { get; private set; }
        public bool SafeHeaders { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests++;
            SafeHeaders = request.Headers.Authorization is null && !request.Headers.Contains("Cookie") && !request.Headers.Contains("Accept-Encoding") && request.Headers.Accept.Single().MediaType == "application/json";
            return _response(cancellationToken);
        }
    }
}

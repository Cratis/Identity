// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text;
using Cratis.Identity.Authorization.TokenExchange;
using Cratis.Identity.Configuration;
using Cratis.Identity.given;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;

namespace Cratis.Identity.for_ExternalIssuerKeys.given;

public class an_issuer_that_serves_keys : Specification
{
    private protected ExternalIssuerFixture _fixture;
    private protected FixedTimeProvider _time;
    private protected ExternalIssuerOptions _issuer;
    private protected ExternalIssuerKeys _keys;
    private protected int _requests;
    private protected Func<HttpResponseMessage> _respond;
    private protected IReadOnlyCollection<SecurityKey>? _result;

    void Establish()
    {
        _fixture = new();
        _time = new(DateTimeOffset.UtcNow);
        _issuer = Configurations.WithTokenExchange().TokenExchange.Issuers[0];
        _respond = () => Json(_fixture.JwksDocument);
        var httpClients = Substitute.For<IHttpClientFactory>();
        httpClients.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient(new Handler(this)));
        _keys = new(httpClients, _time, NullLogger<ExternalIssuerKeys>.Instance);
    }

    void Destroy() => _fixture.Dispose();

    private protected static HttpResponseMessage Json(string body, string mediaType = "application/json") =>
        new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, mediaType) };

    private protected async Task Get(string? keyId = null) => _result = await _keys.Get(_issuer, keyId, TestContext.Current.CancellationToken);

    sealed class Handler(an_issuer_that_serves_keys owner) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            owner._requests++;
            return Task.FromResult(owner._respond());
        }
    }
}

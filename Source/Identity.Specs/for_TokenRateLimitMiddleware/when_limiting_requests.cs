// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Identity.Authorization.RateLimiting;
using Cratis.Identity.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Cratis.Identity.for_TokenRateLimitMiddleware;

public class when_limiting_requests : Specification
{
    DefaultHttpContext _client;
    DefaultHttpContext _source;
    string _clientBody;
    string _sourceBody;
    bool _allowedUntouched;
    bool _otherPath;
    bool _disabled;
    bool _getDoesNotSpendClient;
    bool _malformedLimited;
    int _downstream;

    async Task Because()
    {
        var options = Options.Create(new IdentityOptions { TokenRateLimit = new() { PerClientRequests = 1 } });
        var limiter = new TokenRequestRateLimiter(TimeProvider.System, options, Substitute.For<ILogger<TokenRequestRateLimiter>>());
        var middleware = new TokenRateLimitMiddleware(context =>
        {
            _downstream++;
            context.Response.StatusCode = 202;
            return Task.CompletedTask;
        });
        var first = Context();
        await middleware.InvokeAsync(first, limiter, options);
        _allowedUntouched = first.Response.StatusCode == 202 && first.Response.Headers.Count == 0;
        _client = Context();
        await middleware.InvokeAsync(_client, limiter, options);
        _clientBody = Encoding.UTF8.GetString(((MemoryStream)_client.Response.Body).ToArray());
        var sourceOptions = Options.Create(new IdentityOptions { TokenRateLimit = new() { PerSourceRequests = 1 } });
        var sourceLimiter = new TokenRequestRateLimiter(TimeProvider.System, sourceOptions, Substitute.For<ILogger<TokenRequestRateLimiter>>());
        await middleware.InvokeAsync(Context(), sourceLimiter, sourceOptions);
        _source = Context();
        await middleware.InvokeAsync(_source, sourceLimiter, sourceOptions);
        _sourceBody = Encoding.UTF8.GetString(((MemoryStream)_source.Response.Body).ToArray());
        var other = Context();
        other.Request.Path = "/healthz";
        await middleware.InvokeAsync(other, limiter, options);
        _otherPath = other.Response.StatusCode == 202;
        options.Value.TokenRateLimit.Enabled = false;
        var disabled = Context();
        await middleware.InvokeAsync(disabled, limiter, options);
        _disabled = disabled.Response.StatusCode == 202;
        options.Value.TokenRateLimit.Enabled = true;
        var get = Context();
        get.Request.Method = "GET";
        await middleware.InvokeAsync(get, limiter, options);
        _getDoesNotSpendClient = get.Response.StatusCode == 202;
        var malformed = Context();
        malformed.Request.Form = null!;
        malformed.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("bad=%invalid"));
        await middleware.InvokeAsync(malformed, sourceLimiter, sourceOptions);
        _malformedLimited = malformed.Response.StatusCode == 429;
    }

    static DefaultHttpContext Context()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/CONNECT/TOKEN";
        context.Request.Method = "POST";
        context.Request.ContentType = "application/x-www-form-urlencoded";
        context.Request.Form = new FormCollection(new Dictionary<string, StringValues> { ["client_id"] = "a", ["client_secret"] = "synthetic-secret" });
        context.Response.Body = new MemoryStream();
        return context;
    }

    [Fact] void should_return_429() => _client.Response.StatusCode.ShouldEqual(429);
    [Fact] void should_set_retry_after() => _client.Response.Headers.RetryAfter.ToString().ShouldEqual("60");
    [Fact] void should_disable_caching() => _client.Response.Headers.CacheControl.ToString().ShouldEqual("no-store");
    [Fact] void should_set_pragma() => _client.Response.Headers.Pragma.ToString().ShouldEqual("no-cache");
    [Fact] void should_use_json() => _client.Response.ContentType.ShouldEqual("application/json");
    [Fact] void should_use_the_oauth_refusal() => _clientBody.ShouldEqual(/*lang=json*/ "{\"error\":\"temporarily_unavailable\",\"error_description\":\"Too many requests. Try again later.\"}");
    [Fact] void should_not_reveal_the_partition() => _sourceBody.ShouldEqual(_clientBody);
    [Fact] void should_not_reveal_the_partition_in_retry_after() => _source.Response.Headers.RetryAfter.ShouldEqual(_client.Response.Headers.RetryAfter);
    [Fact] void should_preserve_downstream_responses() => _allowedUntouched.ShouldBeTrue();
    [Fact] void should_pass_other_paths() => _otherPath.ShouldBeTrue();
    [Fact] void should_pass_when_disabled() => _disabled.ShouldBeTrue();
    [Fact] void should_not_count_get_per_client() => _getDoesNotSpendClient.ShouldBeTrue();
    [Fact] void should_still_limit_a_malformed_form_by_source() => _malformedLimited.ShouldBeTrue();
    [Fact] void should_not_call_downstream_on_refusal() => _downstream.ShouldEqual(5);
}

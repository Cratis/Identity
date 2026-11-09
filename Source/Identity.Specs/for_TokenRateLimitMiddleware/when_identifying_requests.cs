// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text;
using Cratis.Identity.Authorization.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Cratis.Identity.for_TokenRateLimitMiddleware;

public class when_identifying_requests : Specification
{
    string? _basic;
    string? _oversized;
    string? _control;
    string? _form;
    string? _get;
    string _prefixA;
    string _prefixB;
    string _mapped;
    string _unknown;

    async Task Because()
    {
        var context = Context("form-client", "2001:db8:1234:5678::1");
        context.Request.Headers.Authorization = $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes("encoded%3Aclient:synthetic-secret"))}";
        var basic = await TokenRequestIdentification.Read(context);
        _basic = basic.ClientId;
        _prefixA = basic.Source;
        _prefixB = (await TokenRequestIdentification.Read(Context("a", "2001:db8:1234:5678::abcd"))).Source;
        _mapped = (await TokenRequestIdentification.Read(Context("a", "::ffff:192.0.2.1"))).Source;
        _oversized = (await TokenRequestIdentification.Read(Context(new string('a', 257), "192.0.2.1"))).ClientId;
        _control = (await TokenRequestIdentification.Read(Context("bad\nclient", "192.0.2.1"))).ClientId;
        _form = (await TokenRequestIdentification.Read(Context("form-client", "192.0.2.1"))).ClientId;
        context.Request.Method = "GET";
        _get = (await TokenRequestIdentification.Read(context)).ClientId;
        _unknown = (await TokenRequestIdentification.Read(new DefaultHttpContext())).Source;
    }

    static DefaultHttpContext Context(string client, string address)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.ContentType = "application/x-www-form-urlencoded";
        context.Request.Form = new FormCollection(new Dictionary<string, StringValues> { ["client_id"] = client });
        context.Connection.RemoteIpAddress = IPAddress.Parse(address);
        context.Request.Headers["X-Forwarded-For"] = "198.51.100.9";
        return context;
    }

    [Fact] void should_prefer_and_url_decode_basic_client_id() => _basic.ShouldEqual("encoded:client");
    [Fact] void should_use_form_without_basic() => _form.ShouldEqual("form-client");
    [Fact] void should_reject_large_client_ids() => _oversized.ShouldBeNull();
    [Fact] void should_reject_control_characters() => _control.ShouldBeNull();
    [Fact] void should_not_identify_get_requests() => _get.ShouldBeNull();
    [Fact] void should_group_ipv6_by_prefix() => _prefixA.ShouldEqual(_prefixB);
    [Fact] void should_normalize_mapped_ipv4_and_ignore_forwarded_headers() => _mapped.ShouldEqual("192.0.2.1");
    [Fact] void should_use_an_unknown_source_when_missing() => _unknown.ShouldEqual("unknown");
}

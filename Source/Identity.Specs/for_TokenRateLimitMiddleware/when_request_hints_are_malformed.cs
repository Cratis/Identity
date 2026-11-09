// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Identity.Authorization.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;

namespace Cratis.Identity.for_TokenRateLimitMiddleware;

public class when_request_hints_are_malformed : Specification
{
    string? _fallback;
    string? _repeated;
    string? _malformed;
    string _source;
    string? _grant;

    async Task Because()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.ContentType = "application/x-www-form-urlencoded";
        context.Request.Headers.Authorization = "Basic not-base64";
        context.Request.Form = new FormCollection(new Dictionary<string, StringValues> { ["client_id"] = "form-client", ["grant_type"] = "refresh_token" });
        var identified = await TokenRequestIdentification.Read(context);
        _fallback = identified.ClientId;
        _grant = identified.GrantType;
        context.Request.Form = new FormCollection(new Dictionary<string, StringValues> { ["client_id"] = new StringValues(["a", "b"]) });
        _repeated = (await TokenRequestIdentification.Read(context)).ClientId;
        var form = Substitute.For<IFormFeature>();
        form.HasFormContentType.Returns(true);
        form.ReadFormAsync(Arg.Any<CancellationToken>()).Returns(Task.FromException<IFormCollection>(new InvalidDataException("Malformed test form.")));
        context.Features.Set(form);
        context.Connection.RemoteIpAddress = IPAddress.Parse("192.0.2.2");
        var malformed = await TokenRequestIdentification.Read(context);
        _malformed = malformed.ClientId;
        _source = malformed.Source;
    }

    [Fact] void should_fall_back_from_malformed_basic() => _fallback.ShouldEqual("form-client");
    [Fact] void should_extract_the_grant() => _grant.ShouldEqual("refresh_token");
    [Fact] void should_reject_repeated_client_fields() => _repeated.ShouldBeNull();
    [Fact] void should_not_throw_for_an_unreadable_form() => _malformed.ShouldBeNull();
    [Fact] void should_preserve_the_source_for_unreadable_forms() => _source.ShouldEqual("192.0.2.2");
}

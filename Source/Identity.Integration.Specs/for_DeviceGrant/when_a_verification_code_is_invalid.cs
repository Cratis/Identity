// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.for_DeviceGrant;

[Collection(IdentityCollection.Name)]
public class when_a_verification_code_is_invalid(IdentityHost host) : IAsyncLifetime
{
    HttpStatusCode[] _statuses;
    string[] _pages;
    string _cache;
    string _referrer;

    public async ValueTask InitializeAsync()
    {
        var browser = host.CreateBrowser();
        using var entry = await browser.Send(new HttpRequestMessage(HttpMethod.Get, "/device"));
        entry.EnsureSuccessStatusCode();
        var page = await entry.Content.ReadAsStringAsync();
        using var malformed = await browser.VerifyDevice(page, "review", "not-a-valid-code");
        using var unknown = await browser.VerifyDevice(page, "review", "ZZZZZZZZ");
        _statuses = [malformed.StatusCode, unknown.StatusCode];
        _pages = [await malformed.Content.ReadAsStringAsync(), await unknown.Content.ReadAsStringAsync()];
        _cache = malformed.Headers.CacheControl?.ToString() ?? string.Empty;
        _referrer = malformed.Headers.GetValues("Referrer-Policy").Single();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact] void should_reject_both_requests() => _statuses.ShouldEqual([HttpStatusCode.BadRequest, HttpStatusCode.BadRequest]);
    [Fact] void should_not_distinguish_unknown_from_malformed_codes() => _pages[0].ShouldEqual(_pages[1]);
    [Fact] void should_use_the_exact_safe_message() => _pages[0].ShouldContain("That code is invalid or has expired. Check the code on your device or start again.");
    [Fact] void should_never_reflect_entered_codes() => _pages.Any(_ => _.Contains("not-a-valid-code", StringComparison.Ordinal) || _.Contains("ZZZZZZZZ", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_prevent_caching() => _cache.ShouldContain("no-store");
    [Fact] void should_prevent_referrer_leakage() => _referrer.ShouldEqual("no-referrer");
}

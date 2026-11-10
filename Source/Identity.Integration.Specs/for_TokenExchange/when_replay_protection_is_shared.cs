// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization.TokenExchange;
using Cratis.Identity.Operational;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Identity.Integration.for_TokenExchange;

[Collection(ExchangeCollection.Name)]
public class when_replay_protection_is_shared(ExchangeIdentityHost host) : Specification
{
    [Fact]
    async Task should_issue_once_across_hosts_and_remember_after_a_host_restarts()
    {
        await LinkAccount();
        var subject = host.External.SubjectToken();
        await using (var replica = host.WithWebHostBuilder(_ => { }))
        using (var secondBrowser = replica.CreateClient())
        {
            var firstBrowser = host.CreateBrowser();
            var replies = await Task.WhenAll(Exchange(firstBrowser.Client, subject), Exchange(secondBrowser, subject));
            replies.Count(_ => _.Status == HttpStatusCode.OK).ShouldEqual(1);
            var refused = replies.Single(_ => _.Status != HttpStatusCode.OK);
            refused.Error.ShouldEqual("invalid_grant");
            refused.Description.ShouldEqual(TokenExchangeGrant.InvalidSubjectDescription);
        }

        await using var restarted = host.WithWebHostBuilder(_ => { });
        using var restartedBrowser = restarted.CreateClient();
        var replay = await Exchange(restartedBrowser, subject);
        replay.Error.ShouldEqual("invalid_grant");
        replay.Description.ShouldEqual(TokenExchangeGrant.InvalidSubjectDescription);
    }

    [Fact]
    async Task should_replace_expired_leftovers_without_waiting_for_ttl_cleanup()
    {
        var database = host.Services.GetRequiredService<IMongoDatabase>();
        var entries = database.GetCollection<BsonDocument>(MongoSubjectTokenReplayGuard.CollectionName);
        var key = Guid.NewGuid().ToString();
        var now = DateTimeOffset.UtcNow;
        await entries.InsertOneAsync(new BsonDocument { ["_id"] = key, ["Until"] = now.AddMinutes(-1).UtcDateTime }, cancellationToken: TestContext.Current.CancellationToken);
        var first = new MongoSubjectTokenReplayGuard(database, TimeProvider.System);
        var second = new MongoSubjectTokenReplayGuard(database, TimeProvider.System);
        var results = await Task.WhenAll(first.Record(key, now.AddMinutes(1), TestContext.Current.CancellationToken), second.Record(key, now.AddMinutes(1), TestContext.Current.CancellationToken));
        results.Count(_ => _ == ReplayRecordResult.Recorded).ShouldEqual(1);
        results.Count(_ => _ == ReplayRecordResult.AlreadyUsed).ShouldEqual(1);
    }

    [Fact]
    async Task should_keep_the_entry_before_expiry_and_replace_it_afterwards()
    {
        var database = host.Services.GetRequiredService<IMongoDatabase>();
        var time = new ReplayTime();
        var guard = new MongoSubjectTokenReplayGuard(database, time);
        var key = Guid.NewGuid().ToString();
        var until = time.Now.AddMinutes(1);
        (await guard.Record(key, until, TestContext.Current.CancellationToken)).ShouldEqual(ReplayRecordResult.Recorded);
        time.Now = until.AddMilliseconds(-1);
        (await guard.Record(key, until.AddMinutes(1), TestContext.Current.CancellationToken)).ShouldEqual(ReplayRecordResult.AlreadyUsed);
        time.Now = until.AddMilliseconds(1);
        (await guard.Record(key, until.AddMinutes(1), TestContext.Current.CancellationToken)).ShouldEqual(ReplayRecordResult.Recorded);
    }

    [Fact]
    async Task should_fail_closed_when_the_replay_store_is_unreachable()
    {
        await LinkAccount();
        var settings = new MongoClientSettings
        {
            Server = new MongoServerAddress("127.0.0.1", 1),
            ServerSelectionTimeout = TimeSpan.FromMilliseconds(100),
            ConnectTimeout = TimeSpan.FromMilliseconds(100)
        };
        using var client = new MongoClient(settings);
        var unavailable = new MongoSubjectTokenReplayGuard(client.GetDatabase("unavailable_replay_specs"), TimeProvider.System);
        await using var replica = host.WithWebHostBuilder(builder => builder.ConfigureServices(services => services.AddSingleton<ISubjectTokenReplayGuard>(unavailable)));
        using var browser = replica.CreateClient();
        var reply = await Exchange(browser, host.External.SubjectToken());
        reply.Status.ShouldNotEqual(HttpStatusCode.OK);
        reply.Error.ShouldEqual("temporarily_unavailable");
        reply.Description.ShouldEqual("Token exchange is unavailable right now. Try again later.");
    }

    async Task LinkAccount()
    {
        var browser = host.CreateBrowser();
        var (verifier, challenge) = Browser.Pkce();
        await browser.RedeemCode(await browser.LoginForCode(challenge), verifier);
    }

    static async Task<(HttpStatusCode Status, string? Error, string? Description)> Exchange(HttpClient browser, string subject)
    {
        using var body = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = TokenExchangeConstants.GrantType,
            ["client_id"] = ExternalIssuer.ClientId,
            ["client_secret"] = ExternalIssuer.ClientSecret,
            ["subject_token"] = subject,
            ["subject_token_type"] = TokenExchangeConstants.JwtTokenType,
            ["resource"] = IdentityHost.DirectMcp,
            ["scope"] = "direct:mcp:read",
            ["tenant"] = IdentityHost.MemberTenant
        });
        using var response = await browser.PostAsync("/connect/token", body, TestContext.Current.CancellationToken);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        return (response.StatusCode,
            json.RootElement.TryGetProperty("error", out var error) ? error.GetString() : null,
            json.RootElement.TryGetProperty("error_description", out var description) ? description.GetString() : null);
    }

    sealed class ReplayTime : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2040, 1, 1, 0, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }
}

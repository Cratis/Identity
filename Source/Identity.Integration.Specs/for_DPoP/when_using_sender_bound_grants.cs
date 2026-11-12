// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using MongoDB.Bson;
using MongoDB.Driver;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.Integration.for_DPoP;

[Collection(DPoPCollection.Name)]
[Requirement(Requirements.DPoPIssuance)]
[Requirement(Requirements.DPoPNonce)]
[Requirement(Requirements.DPoPRefresh)]
[Requirement(Requirements.DPoPReplay)]
[Requirement(Requirements.DPoPProof)]
[Requirement(Requirements.DPoPMetadata)]
public class when_using_sender_bound_grants(DPoPHost host)
{
    [Fact]
    public async Task should_challenge_without_consuming_the_code_then_issue_bound_tokens()
    {
        using var sender = new DPoPSender();
        var browser = host.CreateBrowser();
        var (verifier, challenge) = Browser.Pkce();
        var fields = DPoPSender.Code(await browser.LoginForCode(challenge), verifier);
        using var nonce = await DPoPSender.Send(browser, fields, sender.Sign());
        nonce.StatusCode.ShouldEqual(HttpStatusCode.BadRequest);
        (await Browser.ReadJson(nonce)).GetProperty("error").GetString().ShouldEqual("use_dpop_nonce");
        nonce.Headers.Contains("DPoP-Nonce").ShouldBeTrue();
        nonce.Headers.CacheControl.NoStore.ShouldBeTrue();
        using var issued = await sender.Prove(browser, fields);
        issued.StatusCode.ShouldEqual(HttpStatusCode.OK);
        var tokens = await Browser.ReadJson(issued);
        tokens.GetProperty("token_type").GetString().ShouldEqual("DPoP");
        Confirmation(tokens).ShouldEqual(sender.Jkt);
        var entry = await Entry(tokens.GetProperty("refresh_token").GetString());
        entry["properties"]["cratis_dpop_jkt"].AsString.ShouldEqual(sender.Jkt);
    }

    [Fact]
    public async Task should_preserve_the_family_binding_across_rotation_and_refuse_missing_or_wrong_keys()
    {
        using var sender = new DPoPSender();
        using var other = new DPoPSender();
        var browser = host.CreateBrowser();
        var tokens = await Issue(browser, sender);
        var refresh = tokens.GetProperty("refresh_token").GetString();
        var fields = DPoPSender.Refresh(refresh);
        using var missing = await DPoPSender.Send(browser, fields, null);
        missing.StatusCode.ShouldEqual(HttpStatusCode.BadRequest);
        using var wrong = await DPoPSender.Send(browser, fields, other.Sign());
        wrong.StatusCode.ShouldEqual(HttpStatusCode.BadRequest);
        (await Browser.ReadJson(wrong)).GetProperty("error_description").GetString().ShouldEqual("jkt_mismatch");
        using var rotated = await sender.Prove(browser, fields);
        rotated.StatusCode.ShouldEqual(HttpStatusCode.OK);
        var successor = await Browser.ReadJson(rotated);
        Confirmation(successor).ShouldEqual(sender.Jkt);
        successor.GetProperty("token_type").GetString().ShouldEqual("DPoP");
        (await Entry(successor.GetProperty("refresh_token").GetString()))["properties"]["cratis_dpop_jkt"].AsString.ShouldEqual(sender.Jkt);
    }

    [Fact]
    public async Task should_refuse_a_replayed_proof_without_consuming_the_successor()
    {
        using var sender = new DPoPSender();
        var browser = host.CreateBrowser();
        var tokens = await Issue(browser, sender);
        var proof = sender.Sign();
        using var rotated = await DPoPSender.Send(browser, DPoPSender.Refresh(tokens.GetProperty("refresh_token").GetString()), proof);
        rotated.StatusCode.ShouldEqual(HttpStatusCode.OK);
        var successor = (await Browser.ReadJson(rotated)).GetProperty("refresh_token").GetString();
        using var replayed = await DPoPSender.Send(browser, DPoPSender.Refresh(successor), proof);
        replayed.StatusCode.ShouldEqual(HttpStatusCode.BadRequest);
        (await Browser.ReadJson(replayed)).GetProperty("error_description").GetString().ShouldEqual("reused_jti");
        using var valid = await sender.Prove(browser, DPoPSender.Refresh(successor));
        valid.StatusCode.ShouldEqual(HttpStatusCode.OK);
    }

    [Fact]
    public async Task should_never_bind_an_existing_bearer_family()
    {
        var browser = host.CreateBrowser();
        var (verifier, challenge) = Browser.Pkce();
        var (_, tokens) = await browser.RedeemCode(await browser.LoginForCode(challenge), verifier);
        var refresh = tokens.GetProperty("refresh_token").GetString();
        using var sender = new DPoPSender();
        using var refused = await DPoPSender.Send(browser, DPoPSender.Refresh(refresh), sender.Sign());
        refused.StatusCode.ShouldEqual(HttpStatusCode.BadRequest);
        (await Browser.ReadJson(refused)).GetProperty("error_description").GetString().ShouldEqual("unbound_family");
        (await browser.Refresh(refresh)).Status.ShouldEqual(HttpStatusCode.OK);
    }

    [Fact]
    public async Task should_use_only_the_configured_public_endpoint()
    {
        using var sender = new DPoPSender();
        var browser = host.CreateBrowser();
        var (verifier, challenge) = Browser.Pkce();
        var fields = DPoPSender.Code(await browser.LoginForCode(challenge), verifier);
        using var refused = await DPoPSender.Send(browser, fields, sender.Sign("https://internal/connect/token"));
        refused.StatusCode.ShouldEqual(HttpStatusCode.BadRequest);
        (await Browser.ReadJson(refused)).GetProperty("error_description").GetString().ShouldEqual("htu_mismatch");
        using var valid = await sender.Prove(browser, fields);
        valid.StatusCode.ShouldEqual(HttpStatusCode.OK);
    }

    [Fact]
    public async Task should_bind_a_device_grant_at_the_approved_poll()
    {
        using var sender = new DPoPSender();

        // Obtain an origin/key nonce before polling; a challenge does not bypass the device poll interval.
        await Issue(host.CreateBrowser(), sender);
        var device = host.CreateBrowser();
        var started = await device.StartDevice();
        var browser = host.CreateBrowser();
        var review = await browser.ReviewDevice(started.Body.GetProperty("user_code").GetString());
        using var approval = await browser.VerifyDevice(review, "approve");
        approval.EnsureSuccessStatusCode();
        using var issued = await sender.Prove(device,
        [
            new("client_id", Browser.ClientId), new("grant_type", "urn:ietf:params:oauth:grant-type:device_code"),
            new("device_code", started.Body.GetProperty("device_code").GetString())
        ]);
        issued.StatusCode.ShouldEqual(HttpStatusCode.OK);
        Confirmation(await Browser.ReadJson(issued)).ShouldEqual(sender.Jkt);
    }

    [Fact]
    public async Task should_advertise_only_es256_on_the_resource_lane_when_enabled()
    {
        using var client = host.CreateClient();
        using var response = await client.GetAsync("/.well-known/oauth-authorization-server", TestContext.Current.CancellationToken);
        var metadata = await Browser.ReadJson(response);
        metadata.GetProperty("dpop_signing_alg_values_supported").EnumerateArray().Select(value => value.GetString()).ShouldContainOnly("ES256");
        using var identityResponse = await client.GetAsync("/.well-known/openid-configuration", TestContext.Current.CancellationToken);
        (await Browser.ReadJson(identityResponse)).TryGetProperty("dpop_signing_alg_values_supported", out _).ShouldBeFalse();
    }

    static string Confirmation(JsonElement tokens)
    {
        var jwt = new JsonWebToken(tokens.GetProperty("access_token").GetString());
        using var confirmation = JsonDocument.Parse(jwt.Claims.Single(claim => claim.Type == "cnf").Value);
        return confirmation.RootElement.GetProperty("jkt").GetString() ?? throw new InvalidOperationException("Missing confirmation key.");
    }

    static async Task<JsonElement> Issue(Browser browser, DPoPSender sender)
    {
        var (verifier, challenge) = Browser.Pkce();
        using var issued = await sender.Prove(browser, DPoPSender.Code(await browser.LoginForCode(challenge), verifier));
        issued.StatusCode.ShouldEqual(HttpStatusCode.OK);
        return await Browser.ReadJson(issued);
    }

    async Task<BsonDocument> Entry(string refreshToken)
    {
        await using var scope = host.Services.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<IOpenIddictServerFactory>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IOpenIddictServerDispatcher>();
        var validation = new ValidateTokenContext(await factory.CreateTransactionAsync()) { Token = refreshToken };
        validation.ValidTokenTypes.Add(OpenIddictConstants.TokenTypeIdentifiers.RefreshToken);
        await dispatcher.DispatchAsync(validation);
        validation.IsRejected.ShouldBeFalse();
        return await host.Database.GetCollection<BsonDocument>("openiddict.tokens").Find(Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(validation.Principal.GetTokenId()))).SingleAsync(TestContext.Current.CancellationToken);
    }
}

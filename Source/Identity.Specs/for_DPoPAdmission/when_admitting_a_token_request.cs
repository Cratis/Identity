// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Identity.Configuration;
using Cratis.Identity.DPoP;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.for_DPoPAdmission;

public class when_admitting_a_token_request : for_DPoPProofValidator.given.a_signed_proof
{
    [Fact]
    async Task should_accept_a_bound_proof_and_reserve_before_binding()
    {
        var request = NewRequest();
        (await request.Admission.Admit(request.Context)).ShouldBeTrue();
        request.Binding.Jkt.ShouldEqual(request.Jkt);
        await request.Replay.Received(1).TryReserve("as", "https://identity.example", request.Jkt, "proof-1", DateTimeOffset.FromUnixTimeSeconds(Now + 390), Arg.Any<CancellationToken>());
    }

    [Fact]
    async Task should_not_burn_a_jti_on_nonce_failure()
    {
        var request = NewRequest(nonce: false);
        (await request.Admission.Admit(request.Context)).ShouldBeFalse();
        request.Context.Error.ShouldEqual("use_dpop_nonce");
        request.Http.Response.Headers.ContainsKey("DPoP-Nonce").ShouldBeTrue();
        await request.Replay.DidNotReceiveWithAnyArgs().TryReserve(default!, default!, default!, default!, default, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("wrong-nonce")]
    async Task should_refuse_invalid_signatures_without_a_nonce_challenge(string? nonce)
    {
        var request = NewRequest(nonce: nonce is not null, nonceValue: nonce);
        var parts = request.Http.Request.Headers["DPoP"].ToString().Split('.');
        parts[2] = new string('A', 86);
        request.Http.Request.Headers["DPoP"] = string.Join('.', parts);
        (await request.Admission.Admit(request.Context)).ShouldBeFalse();
        request.Context.Error.ShouldEqual("invalid_dpop_proof");
        request.Context.ErrorDescription.ShouldEqual("invalid_signature");
        request.Http.Response.Headers.ContainsKey("DPoP-Nonce").ShouldBeFalse();
        request.Binding.Jkt.ShouldBeNull();
        await request.Replay.DidNotReceiveWithAnyArgs().TryReserve(default!, default!, default!, default!, default, Arg.Any<CancellationToken>());
    }

    [Fact]
    async Task should_challenge_a_valid_proof_with_a_wrong_nonce_without_reserving()
    {
        var request = NewRequest(nonceValue: "wrong-nonce");
        (await request.Admission.Admit(request.Context)).ShouldBeFalse();
        request.Context.Error.ShouldEqual("use_dpop_nonce");
        var challenge = request.Http.Response.Headers["DPoP-Nonce"].ToString();
        request.Nonces.Validate(challenge, "https://identity.example", request.Jkt).ShouldBeTrue();
        request.Binding.Jkt.ShouldBeNull();
        await request.Replay.DidNotReceiveWithAnyArgs().TryReserve(default!, default!, default!, default!, default, Arg.Any<CancellationToken>());
    }

    [Fact]
    async Task should_refuse_wrong_key_without_reserving()
    {
        var request = NewRequest();
        (await request.Admission.Admit(request.Context, true, "another-key")).ShouldBeFalse();
        request.Context.ErrorDescription.ShouldEqual("jkt_mismatch");
        await request.Replay.DidNotReceiveWithAnyArgs().TryReserve(default!, default!, default!, default!, default, Arg.Any<CancellationToken>());
    }

    [Fact]
    async Task should_never_bind_an_unbound_refresh_family()
    {
        var request = NewRequest();
        (await request.Admission.Admit(request.Context, refresh: true)).ShouldBeFalse();
        request.Context.ErrorDescription.ShouldEqual("unbound_family");
    }

    [Fact]
    async Task should_require_a_proof_for_a_bound_family()
    {
        var request = NewRequest();
        request.Http.Request.Headers.Remove("DPoP");
        (await request.Admission.Admit(request.Context, true, request.Jkt)).ShouldBeFalse();
        request.Context.ErrorDescription.ShouldEqual("missing_proof");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    async Task should_require_a_proof_for_global_and_client_policy(bool global)
    {
        var request = NewRequest();
        request.Http.Request.Headers.Remove("DPoP");
        request.Options.DPoP.RequireForAllClients = global;
        if (!global) request.Options.DPoP.RequiredClients.Add("cratis-cli");
        (await request.Admission.Admit(request.Context)).ShouldBeFalse();
        request.Context.Error.ShouldEqual("invalid_dpop_proof");
    }

    [Fact]
    async Task should_refuse_existing_unbound_families_when_policy_changes()
    {
        var request = NewRequest();
        request.Http.Request.Headers.Remove("DPoP");
        request.Options.DPoP.RequireForAllClients = true;
        (await request.Admission.Admit(request.Context, refresh: true)).ShouldBeFalse();
    }

    [Fact]
    async Task should_leave_optional_unbound_grants_unchanged()
    {
        var request = NewRequest();
        request.Http.Request.Headers.Remove("DPoP");
        (await request.Admission.Admit(request.Context)).ShouldBeTrue();
    }

    [Fact]
    async Task should_fail_closed_when_the_shared_store_fails()
    {
        var request = NewRequest();
        request.Replay.TryReserve(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<bool>(new InvalidOperationException()));
        (await request.Admission.Admit(request.Context)).ShouldBeFalse();
        request.Context.ErrorDescription.ShouldEqual("jti_cache_unavailable");
        request.Binding.Jkt.ShouldBeNull();
    }

    [Fact]
    async Task should_refuse_replay_without_installing_a_binding()
    {
        var request = NewRequest();
        request.Replay.TryReserve(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(false);
        (await request.Admission.Admit(request.Context)).ShouldBeFalse();
        request.Context.ErrorDescription.ShouldEqual("reused_jti");
        request.Binding.Jkt.ShouldBeNull();
    }

    Request NewRequest(bool nonce = true, string? nonceValue = null)
    {
        var replay = Substitute.For<IDPoPReplayStore>();
        replay.TryReserve(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(true);
        var time = new Clock();
        var nonces = new DPoPNonce(new EphemeralDataProtectionProvider(), time);
        var binding = new DPoPBinding();
        var options = new IdentityOptions { DPoP = new DPoPOptions { Enabled = true, PublicTokenEndpoint = Endpoint } };
        var admission = new DPoPAdmission(_validator, nonces, replay, binding, Options.Create(options), time);
        var http = new DefaultHttpContext();
        var transaction = new OpenIddictServerTransaction { Logger = NullLogger.Instance, Options = new OpenIddictServerOptions(), Request = new OpenIddictRequest { ClientId = "cratis-cli" } };
        transaction.SetProperty(typeof(HttpRequest).FullName, new WeakReference<HttpRequest>(http.Request));
        var jkt = _validator.Validate([Sign()], Endpoint).Proof.Jkt;
        var payload = new Dictionary<string, object>(_payload);
        if (nonce) payload["nonce"] = nonceValue ?? nonces.Issue("https://identity.example", jkt);
        http.Request.Headers["DPoP"] = Sign(payload: JsonSerializer.Serialize(payload));
        return new(admission, replay, binding, new ProcessSignInContext(transaction), http, options, jkt, nonces);
    }

    sealed record Request(DPoPAdmission Admission, IDPoPReplayStore Replay, DPoPBinding Binding, ProcessSignInContext Context, DefaultHttpContext Http, IdentityOptions Options, string Jkt, DPoPNonce Nonces);

    sealed class Clock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => DateTimeOffset.FromUnixTimeSeconds(Now);
    }
}

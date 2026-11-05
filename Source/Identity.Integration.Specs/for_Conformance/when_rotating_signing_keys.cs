// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Cratis.Identity.Integration.for_Conformance;

/// <summary>
/// Rotates signing and encryption keys through the secret store while the host runs: publish, overlap, then retire.
/// </summary>
/// <param name="host">The <see cref="RotatingIdentityHost"/>.</param>
[Requirement(Requirements.KeyRotationOverlap)]
[Collection(RotationCollection.Name)]
public class when_rotating_signing_keys(RotatingIdentityHost host) : IAsyncLifetime
{
    static readonly TimeSpan _publication = TimeSpan.FromSeconds(10);

    /// <summary>
    /// The host is shared by every fact of this class and the scenario changes its secret store for good (it retires key A),
    /// so the scenario runs once per host and each fact asserts on that one outcome.
    /// </summary>
    static readonly ConditionalWeakTable<RotatingIdentityHost, Task<Outcome>> _outcomes = [];

    string _kidA;
    string _kidB;
    string _kidC;
    string _t1Kid;
    string _t2Kid;
    string _t3Kid;
    HttpStatusCode _r1Status;
    List<string> _afterB;
    List<string> _afterC;
    List<string> _afterRemovingA;
    bool _t1AcceptedAfterB;

    public async ValueTask InitializeAsync()
    {
        var outcome = await _outcomes.GetValue(host, Run);
        _kidA = outcome.KidA;
        _kidB = outcome.KidB;
        _kidC = outcome.KidC;
        _t1Kid = outcome.T1Kid;
        _t2Kid = outcome.T2Kid;
        _t3Kid = outcome.T3Kid;
        _r1Status = outcome.R1Status;
        _afterB = outcome.AfterB;
        _afterC = outcome.AfterC;
        _afterRemovingA = outcome.AfterRemovingA;
        _t1AcceptedAfterB = outcome.T1AcceptedAfterB;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact] void should_sign_with_the_first_key_initially() => Conformance.Conformance.Expect(Requirements.KeyRotationOverlap, () => _t1Kid.ShouldEqual(_kidA));
    [Fact] void should_publish_both_keys_during_the_overlap() => Conformance.Conformance.Expect(Requirements.KeyRotationOverlap, () => _afterB.ShouldContainOnly(_kidA, _kidB));
    [Fact] void should_still_accept_a_token_signed_by_the_old_key() => Conformance.Conformance.Expect(Requirements.KeyRotationOverlap, () => _t1AcceptedAfterB.ShouldBeTrue());
    [Fact] void should_still_redeem_a_refresh_token_protected_by_the_old_key() => Conformance.Conformance.Expect(Requirements.KeyRotationOverlap, () => _r1Status.ShouldEqual(HttpStatusCode.OK));
    [Fact] void should_sign_with_the_new_key_once_it_is_eligible() => Conformance.Conformance.Expect(Requirements.KeyRotationOverlap, () => _t2Kid.ShouldEqual(_kidB));
    [Fact] void should_publish_a_key_before_it_signs() => Conformance.Conformance.Expect(Requirements.KeyRotationOverlap, () => _afterC.ShouldContain(_kidC));
    [Fact] void should_not_sign_with_a_key_that_is_not_yet_eligible() => Conformance.Conformance.Expect(Requirements.KeyRotationOverlap, () => _t3Kid.ShouldEqual(_kidB));
    [Fact] void should_stop_publishing_a_retired_key() => Conformance.Conformance.Expect(Requirements.KeyRotationOverlap, () => _afterRemovingA.ShouldContainOnly(_kidB, _kidC));

    static string Required(HttpStatusCode status, JsonElement body, string property) =>
        body.TryGetProperty(property, out var value) && value.GetString() is { } text
            ? text
            : throw new InvalidOperationException($"The token endpoint answered {(int)status} without \"{property}\": {body.GetRawText()}");

    static async Task<Outcome> Run(RotatingIdentityHost host)
    {
        var kidA = host.InitialSigningKid;
        var browser = host.CreateBrowser();
        using var client = browser.Client;
        var (verifier, challenge) = Browser.Pkce();
        var (issuedStatus, issued) = await browser.RedeemCode(await browser.LoginForCode(challenge), verifier);
        var t1 = Required(issuedStatus, issued, "access_token");
        var r1 = Required(issuedStatus, issued, "refresh_token");
        var t1Kid = KidOf(t1);

        // B and its encryption key are published ahead of time, which is also when they start to sign.
        var kidB = host.Secrets.WriteKey("signing", "b", DateTimeOffset.UtcNow.AddMinutes(-11));
        host.Secrets.WriteKey("encryption", "b", DateTimeOffset.UtcNow.AddMinutes(-11));
        var afterB = await WaitForKeys(browser, _ => _.Contains(kidB));
        var t1AcceptedAfterB = (await AccessTokens.Validate(browser, t1)).IsValid;
        var (r1Status, second) = await browser.Refresh(r1);
        var t2Kid = KidOf(Required(r1Status, second, "access_token"));

        // C is published but not yet eligible: it must not sign.
        var kidC = host.Secrets.WriteKey("signing", "c", DateTimeOffset.UtcNow);
        var afterC = await WaitForKeys(browser, _ => _.Contains(kidC));
        var (thirdStatus, third) = await browser.Refresh(Required(r1Status, second, "refresh_token"));
        var t3Kid = KidOf(Required(thirdStatus, third, "access_token"));

        host.Secrets.RemoveKey("signing", "a");
        var afterRemovingA = await WaitForKeys(browser, _ => !_.Contains(kidA));
        return new(kidA, kidB, kidC, t1Kid, t2Kid, t3Kid, r1Status, afterB, afterC, afterRemovingA, t1AcceptedAfterB);
    }

    sealed record Outcome(
        string KidA,
        string KidB,
        string KidC,
        string T1Kid,
        string T2Kid,
        string T3Kid,
        HttpStatusCode R1Status,
        List<string> AfterB,
        List<string> AfterC,
        List<string> AfterRemovingA,
        bool T1AcceptedAfterB);

    static string KidOf(string accessToken) => new JsonWebToken(accessToken).Kid;

    static async Task<List<string>> WaitForKeys(Browser browser, Func<List<string>, bool> condition)
    {
        var deadline = DateTimeOffset.UtcNow + _publication;
        List<string> kids;
        do
        {
            var jwks = new JsonWebKeySet(await browser.Client.GetStringAsync("/.well-known/jwks"));
            kids = jwks.Keys.Select(_ => _.Kid.ToUpperInvariant()).ToList();
            if (condition(kids))
            {
                break;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250));
        }
        while (DateTimeOffset.UtcNow < deadline);

        return kids;
    }
}

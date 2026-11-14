// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Identity.Accounts;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Revocation;
using OpenIddict.Abstractions;

namespace Cratis.Identity.for_RefreshGrantAdmission;

public class when_replaying_the_unlink_vector : given.a_refresh_admission
{
    JsonElement _vector;
    Dictionary<string, bool> _admitted;
    Dictionary<string, string> _expected;

    async Task Because()
    {
        _vector = JsonSerializer.Deserialize<JsonElement>(await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Vectors", "identity-contract-v1", "unlink-revokes-authenticating-grants.json")));
        var given = _vector.GetProperty("given");
        var accountJson = given.GetProperty("accounts")[0];
        var links = new Dictionary<string, Guid>();
        LinkedUpstreamKey Key(JsonElement key)
        {
            var link = Guid.NewGuid();
            links[$"{key.GetProperty("idp").GetString()}|{key.GetProperty("idp_sub").GetString()}"] = link;
            return new(link, new ConnectionId(Guid.NewGuid()), key.GetProperty("idp").GetString()!, key.GetProperty("idp_sub").GetString()!, key.GetProperty("latest_observed_login").GetString(), DateTimeOffset.UnixEpoch);
        }

        _ = accountJson.GetProperty("unlinked_keys").EnumerateArray().Select(Key).ToList();
        var linked = accountJson.GetProperty("linked_keys").EnumerateArray().Select(Key).ToArray();
        var account = new Account(new(accountJson.GetProperty("subject").GetString()!), "Member", null, "42", "member", AccountStatus.Active, linked);

        _admitted = [];
        _expected = [];
        foreach (var grant in given.GetProperty("grants").EnumerateArray())
        {
            var id = grant.GetProperty("grant").GetString()!;
            var via = grant.GetProperty("authenticated_via");
            _expected[id] = _vector.GetProperty("then").GetProperty(id).GetProperty("outcome").GetString()!;
            var principal = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity());
            principal.SetAuthorizationId(id);
            StateOf(id, links[$"{via.GetProperty("idp").GetString()}|{via.GetProperty("idp_sub").GetString()}"]);
            _admitted[id] = await _admission.IsValid(principal, _request, account, CancellationToken.None);
        }
    }

    [Fact] void should_refuse_the_grant_authenticated_through_the_unlinked_key() => _admitted["grant-github"].ShouldBeFalse();
    [Fact] void should_fence_that_grants_family() => _fences.Received(1).Commit("grant-github", Arg.Any<string>(), RevocationReason.Revoked, Arg.Any<CancellationToken>());
    [Fact] void should_refresh_the_grant_authenticated_through_the_remaining_link() => _admitted["grant-directory"].ShouldBeTrue();
    [Fact] void should_not_fence_the_remaining_grant() => _fences.DidNotReceive().Commit("grant-directory", Arg.Any<string>(), Arg.Any<RevocationReason>(), Arg.Any<CancellationToken>());
    [Fact] void should_match_the_vector_outcomes() => _expected.All(pair => _admitted[pair.Key] == (pair.Value == "refreshed")).ShouldBeTrue();
}

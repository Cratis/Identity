// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;
using Cratis.Identity.Upstream;

namespace Cratis.Identity.Accounts.given;

public class a_contract_vector : an_account_store
{
    protected JsonElement _given;
    protected JsonElement _when;
    protected JsonElement _then;

    protected static string Text(JsonElement element, string name) => element.GetProperty(name).GetString()!;

    protected async Task Load(string name)
    {
        var vector = JsonSerializer.Deserialize<JsonElement>(await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Vectors", "identity-contract-v1", $"{name}.json")));
        _given = vector.GetProperty("given");
        _when = vector.GetProperty("when");
        _then = vector.GetProperty("then");
        if (!_given.TryGetProperty("accounts", out var accounts))
        {
            return;
        }

        foreach (var account in accounts.EnumerateArray())
        {
            AccountSubject subject = new(Text(account, "subject"));
            var key = account.GetProperty("linked_keys")[0];
            var link = await Seed(subject, new(Text(key, "idp")), new(Text(key, "idp_sub")));
            if (account.TryGetProperty("profile", out var profile))
            {
                var facts = new List<object>();
                if (profile.TryGetProperty("email", out var email))
                {
                    facts.Add(new AccountEmailChanged(new(email.GetString())));
                }

                if (profile.TryGetProperty("login", out var login))
                {
                    facts.Add(new UpstreamKeyDisplayIdentifierObserved(link, new(login.GetString()), _clock.Now));
                }

                await _events.EventLog.AppendMany(subject.Value, facts);
            }
        }

        await Refresh();
    }

    protected VerifiedAuthentication VectorProof(FactId operation, ClaimsPrincipal? session = null)
    {
        var proof = _when.GetProperty("verified_authentication");
        _clock.Now = DateTimeOffset.FromUnixTimeSeconds(proof.GetProperty("auth_time").GetInt64());
        return Verified(new(Text(proof, "idp")), new(Text(proof, "idp_sub")), operation, session);
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cratis.Identity.Contracts.Clients.Events;

namespace Cratis.Identity.Clients.for_EventSourcedClientRegistrations;

public class when_inspecting_appended_payloads : given.a_client_registration_store
{
    string _json;
    async Task Because()
    {
        await Reconcile(_confidential);
        _json = JsonSerializer.Serialize(_store.Appended);
    }

    [Fact] void should_not_contain_the_raw_secret() => _json.Contains(Secret, StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_contain_the_encoded_secret() => _json.Contains(Convert.ToBase64String(Encoding.UTF8.GetBytes(Secret)), StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_contain_unkeyed_hex() => _json.Contains(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Secret))), StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
    [Fact] void should_not_contain_unkeyed_base64() => _json.Contains(Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(Secret))), StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_contain_only_a_keyed_comparand() => _store.Appended.OfType<ClientSecretRotated>().Single().ClientSecretHash.Value.StartsWith("v1.", StringComparison.Ordinal).ShouldBeTrue();
}

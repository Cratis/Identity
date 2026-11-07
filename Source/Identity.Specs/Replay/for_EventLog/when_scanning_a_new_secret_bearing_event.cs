// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Identity.Replay.for_EventLog.given;

namespace Cratis.Identity.Replay.for_EventLog;

public class when_scanning_a_new_secret_bearing_event : Specification
{
    SyntheticSecretEvent _event;

    void Establish() => _event = new(
        (string)EventSeeder.Value(typeof(string)), (string)EventSeeder.Value(typeof(string)));

    [Fact]
    void should_reject_pascal_case_and_snake_case_properties_with_generic_seed_values() =>
        Scan(new { eventType = nameof(SyntheticSecretEvent), payload = _event })
            .ShouldEqual((nameof(SyntheticSecretEvent), "secret-property"), (nameof(SyntheticSecretEvent), "secret-property"));

    [Fact]
    void should_reject_camel_case_properties_with_generic_seed_values() =>
        Scan(new { eventType = nameof(SyntheticSecretEvent), payload = _event }, new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
            .Count.ShouldEqual(2);

    [Fact]
    void should_match_normalized_substrings_and_non_string_values() =>
        Scan(new { payload = new Dictionary<string, object> { ["new-private_key-material"] = new { value = 1 }, ["pendingAuthorizationCode"] = 1, ["nested_client-secret-value"] = "x" } })
            .Count.ShouldEqual(3);

    [Fact]
    void should_ignore_empty_or_null_secret_properties() =>
        Scan(new { AccessToken = string.Empty, client_secret = (string?)null }).ShouldBeEmpty();

    static IReadOnlyList<(string EventType, string Rule)> Scan(object value, JsonSerializerOptions? options = null) =>
        EventLogScanner.Findings([JsonSerializer.Serialize(value, options)], []);
}

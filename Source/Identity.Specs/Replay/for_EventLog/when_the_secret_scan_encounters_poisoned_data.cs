// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Identity.Replay.for_EventLog;

public class when_the_secret_scan_encounters_poisoned_data : Specification
{
    static string[] Rules(object poisoned) => given.EventLogScanner.Findings([JsonSerializer.Serialize(new { eventType = "poisoned-test-event", content = poisoned })], given.Secrets.All).Select(finding => finding.Rule).ToArray();

    [Fact] void should_detect_a_jwt() => Rules(new { value = given.Secrets.Jwt }).ShouldContain("jwt");
    [Fact] void should_detect_a_pat() => Rules(new { value = given.Secrets.Pat }).ShouldContain("pat");
    [Fact] void should_detect_an_upstream_token() => Rules(new { value = "ghp_abcdefghijklmnopqrstuvwx" }).ShouldContain("github");
    [Fact] void should_detect_a_bearer_header() => Rules(new { value = "Bearer abcdefghijklmnopqrstuvwxyz" }).ShouldContain("authorization");
    [Fact] void should_detect_a_dpop_header() => Rules(new { value = "DPoP abcdefghijklmnopqrstuvwxyz" }).ShouldContain("authorization");
    [Fact] void should_detect_pem_material() => Rules(new { value = "-----BEGIN FAKE KEY-----" }).ShouldContain("pem");
    [Fact] void should_detect_secret_properties() => Rules(new { client_secret = "nonempty" }).ShouldContain("secret-property");
    [Fact] void should_detect_opaque_handles() => Rules(new { value = given.Secrets.Opaque }).ShouldContain("opaque");
    [Fact] void should_detect_the_exact_synthetic_inputs() => given.Secrets.All.All(secret => Rules(new { value = secret }).Contains("known-secret")).ShouldBeTrue();
    [Fact] void should_reject_an_unseedable_event_kind() => Catch.Exception(() => given.EventSeeder.Value(typeof(Uri))).ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_not_disclose_the_poisoned_value_in_diagnostics() => given.EventLogScanner.Findings([JsonSerializer.Serialize(new { eventType = "poisoned", value = given.Secrets.Access })], given.Secrets.All).Single().ShouldEqual(("poisoned", "known-secret"));
}

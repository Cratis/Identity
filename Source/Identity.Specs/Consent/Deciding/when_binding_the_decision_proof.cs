// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.ConsentFlow.Deciding;

public class when_binding_the_decision_proof : Pending.given.a_consent_interaction
{
    [Fact] void should_accept_the_original_proof() => proof.Validate(token, request).ShouldBeTrue();
    [Fact] void should_refuse_missing_proof() => proof.Validate(null, request).ShouldBeFalse();
    [Fact] void should_refuse_empty_proof() => proof.Validate(string.Empty, request).ShouldBeFalse();
    [Fact] void should_refuse_altered_proof() => proof.Validate(token + "tamper", request).ShouldBeFalse();
    [Fact] void should_bind_the_request() => proof.Validate(token, request with { Id = ConsentRequestId.New() }).ShouldBeFalse();
    [Fact] void should_bind_the_account() => proof.Validate(token, request with { Binding = binding with { Account = "00000000-0000-4000-8000-000000000099" } }).ShouldBeFalse();
    [Fact] void should_bind_the_session() => proof.Validate(token, request with { Binding = binding with { Session = "different" } }).ShouldBeFalse();
    [Fact] void should_bind_the_realm() => proof.Validate(token, request with { Binding = binding with { Realm = "different" } }).ShouldBeFalse();
    [Fact] void should_bind_the_expiry() => proof.Validate(token, request with { Deadline = request.Deadline.AddSeconds(1) }).ShouldBeFalse();
    [Fact] void should_refuse_at_expiry() => proof.Validate(token, request with { Deadline = clock.Now }).ShouldBeFalse();
}

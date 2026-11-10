// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Events;
using Cratis.Concepts;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;

namespace Cratis.Identity.Accounts.for_AccountContracts;

public class when_scanning_payloads : Specification
{
    Type[] _events;
    string[] _violations;

    void Establish() => _events = typeof(AccountRegistered).Assembly.GetTypes()
        .Where(type => type.Namespace == typeof(AccountRegistered).Namespace && type.IsDefined(typeof(EventTypeAttribute))).ToArray();
    void Because() => _violations = _events.SelectMany(given.AccountPayloadScanner.Violations).ToArray();

    [Fact] void should_have_no_raw_personal_or_secret_strings() => _violations.ShouldBeEmpty();
    [Fact] void should_reject_a_raw_sensitive_fixture() => given.AccountPayloadScanner.Violations(typeof(UnsafePayload)).Count().ShouldEqual(5);
    [Fact] void should_preserve_the_raw_string_guard_for_innocuous_names() => given.AccountPayloadScanner.Violations(typeof(RawText)).ShouldContain("RawText.Description");
    [Fact] void should_preserve_the_missing_compliance_details_guard() => given.AccountPayloadScanner.Violations(typeof(MissingDetails)).ShouldContain("MissingDetails.Description");
    [Fact] void should_have_distinct_default_ids() => _events.Select(type => type.Name).Distinct().Count().ShouldEqual(_events.Length);
    [Fact] void should_not_declare_explicit_ids() => _events.All(type => type.GetCustomAttribute<EventTypeAttribute>().Id.Value.Length == 0).ShouldBeTrue();
    [Fact] void should_use_generation_one() => _events.All(type => type.GetCustomAttribute<EventTypeAttribute>().Generation.Value == 1).ShouldBeTrue();
    [Fact] void should_mark_every_personal_concept() => new[] { typeof(Email), typeof(PersonalName), typeof(Login), typeof(UpstreamSubject), typeof(UpstreamLookupDigest) }.All(type => type.IsDefined(typeof(PIIAttribute)) && type.IsDefined(typeof(ComplianceDetailsAttribute))).ShouldBeTrue();
    [Fact] void should_keep_the_raw_subject_companion_internal() => typeof(UpstreamKeySubjectRecorded).IsDefined(typeof(PublicAttribute)).ShouldBeFalse();
    [Fact] void should_keep_display_observation_internal() => typeof(UpstreamKeyDisplayIdentifierObserved).IsDefined(typeof(PublicAttribute)).ShouldBeFalse();
    [Fact] void should_not_add_fact_id_to_public_payloads() => _events.Where(type => type.IsDefined(typeof(PublicAttribute))).All(type => type.GetProperty(nameof(FactId)) is null).ShouldBeTrue();

    [Fact] void should_keep_erasure_payloads_free_of_personal_fields() => new[] { typeof(AccountErasureRequested), typeof(AccountErasureFailed), typeof(AccountErased), typeof(AccountErasureRecorded) }.All(type => !given.AccountPayloadScanner.Violations(type).Any() && type.GetProperties().All(property => !property.IsDefined(typeof(PIIAttribute)) && !property.PropertyType.IsDefined(typeof(PIIAttribute)))).ShouldBeTrue();
    [Fact] void should_publish_erasure_completion_publicly_and_keep_requests_and_failures_internal() => (typeof(AccountErased).IsDefined(typeof(PublicAttribute)) && !typeof(AccountErasureRequested).IsDefined(typeof(PublicAttribute)) && !typeof(AccountErasureFailed).IsDefined(typeof(PublicAttribute)) && !typeof(AccountErasureRecorded).IsDefined(typeof(PublicAttribute))).ShouldBeTrue();

    sealed record RawText(string Description);
    sealed record MissingDetails(UndocumentedPii Description);
    [PII]
    sealed record UndocumentedPii(string Value) : ConceptAs<string>(Value);
}

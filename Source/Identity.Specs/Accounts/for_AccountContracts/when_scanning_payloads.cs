// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Events;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;

namespace Cratis.Identity.Accounts.for_AccountContracts;

public class when_scanning_payloads : Specification
{
    Type[] _events;
    string[] _violations;

    void Establish() => _events = typeof(AccountRegistered).Assembly.GetTypes().Where(type => type.IsDefined(typeof(EventTypeAttribute))).ToArray();
    void Because() => _violations = _events.SelectMany(given.AccountPayloadScanner.Violations).ToArray();

    [Fact] void should_have_no_raw_personal_or_secret_strings() => _violations.ShouldBeEmpty();
    [Fact] void should_reject_a_raw_sensitive_fixture() => given.AccountPayloadScanner.Violations(typeof(UnsafePayload)).Count().ShouldEqual(5);
    [Fact] void should_have_distinct_explicit_ids() => _events.Select(type => type.GetCustomAttribute<EventTypeAttribute>()!.Id.Value).Distinct().Count().ShouldEqual(_events.Length);
    [Fact] void should_use_generation_one() => _events.All(type => type.GetCustomAttribute<EventTypeAttribute>()!.Generation.Value == 1).ShouldBeTrue();
    [Fact] void should_mark_every_personal_concept() => new[] { typeof(Email), typeof(PersonalName), typeof(Login), typeof(UpstreamSubject), typeof(UpstreamLookupDigest) }.All(type => type.IsDefined(typeof(PIIAttribute)) && type.IsDefined(typeof(ComplianceDetailsAttribute))).ShouldBeTrue();
    [Fact] void should_keep_the_raw_subject_companion_internal() => typeof(UpstreamKeySubjectRecorded).IsDefined(typeof(PublicAttribute)).ShouldBeFalse();
    [Fact] void should_keep_display_observation_internal() => typeof(UpstreamKeyDisplayIdentifierObserved).IsDefined(typeof(PublicAttribute)).ShouldBeFalse();
    [Fact] void should_not_add_fact_id_to_public_payloads() => _events.Where(type => type.IsDefined(typeof(PublicAttribute))).All(type => type.GetProperty(nameof(FactId)) is null).ShouldBeTrue();
}

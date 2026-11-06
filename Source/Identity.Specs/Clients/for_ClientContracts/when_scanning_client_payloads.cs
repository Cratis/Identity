// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Identity.Contracts.Clients.Events;

namespace Cratis.Identity.Clients.for_ClientContracts;

public class when_scanning_client_payloads : Specification
{
    Type[] _events;

    void Establish() => _events = typeof(ClientRegistered).Assembly.GetTypes()
        .Where(type => type.Namespace == typeof(ClientRegistered).Namespace && type.IsDefined(typeof(EventTypeAttribute))).ToArray();

    [Fact] void should_find_all_four_internal_facts() => _events.Length.ShouldEqual(4);
    [Fact] void should_have_no_unsafe_members() => _events.SelectMany(given.ClientPayloadScanner.Violations).ShouldBeEmpty();
    [Fact] void should_reject_the_hostile_fixture() => given.ClientPayloadScanner.Violations(typeof(UnsafeClientPayload)).Order().ToArray().ShouldEqual(["ClientSecret", "RedirectUri", "Secret"]);
    [Fact] void should_have_distinct_ids() => _events.Select(type => type.GetCustomAttribute<EventTypeAttribute>()!.Id.Value).Distinct().Count().ShouldEqual(_events.Length);
    [Fact] void should_have_explicit_lowercase_v4_ids() => _events.All(type => IsVersionFour(type.GetCustomAttribute<EventTypeAttribute>()!.Id.Value)).ShouldBeTrue();
    [Fact] void should_use_generation_one() => _events.All(type => type.GetCustomAttribute<EventTypeAttribute>()!.Generation.Value == 1).ShouldBeTrue();
    [Fact] void should_mark_registration_unique() => typeof(ClientRegistered).IsDefined(typeof(UniqueAttribute)).ShouldBeTrue();

    static bool IsVersionFour(string id) => Guid.TryParseExact(id, "D", out _) && !id.Any(char.IsUpper) && id[14] == '4' && "89ab".Contains(id[19], StringComparison.Ordinal);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts.Events;
using Cratis.Identity.Revocation;

namespace Cratis.Identity.Contracts.Check.for_EventContentRules;

public class when_checking_catalogued_production_events : Specification
{
    IReadOnlyList<Type> _events;
    StringWriter _output;
    int _result;

    void Establish()
    {
        _events = ReflectionShapeBuilder.Discover([typeof(AccountRegistered).Assembly, typeof(GrantId).Assembly]);
        _output = new StringWriter();
    }

    void Because() => _result = CheckRunner.Run(_events, _output);

    [Fact] void should_accept_every_catalogued_production_event() => _result.ShouldEqual(0);
    [Fact] void should_include_the_seven_revocation_events() => _events.Count(type => type.Namespace == "Cratis.Identity.Revocation.Events").ShouldEqual(7);
    [Fact] void should_report_no_schema_violations() => _output.ToString().ShouldEqual($"event-content checked {_events.Count} event shapes; 0 violations{Environment.NewLine}");
}

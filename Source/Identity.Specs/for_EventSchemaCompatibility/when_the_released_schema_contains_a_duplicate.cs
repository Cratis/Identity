// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts.Events;
using Cratis.Identity.Contracts.Check;

namespace Cratis.Identity.for_EventSchemaCompatibility;

public class when_the_released_schema_contains_a_duplicate : Specification
{
    EventSchema _original;
    ReleasedSchemaState _released;
    ReleasedSchemaState _current;
    IReadOnlyList<Violation> _violations;

    void Establish()
    {
        _original = SchemaCompatibility.Describe(typeof(AccountRegistered), ReflectionShapeBuilder.Build(typeof(AccountRegistered)));
        var fixtures = new Dictionary<string, byte[]> { [_original.Fixture] = "{}"u8.ToArray() };
        _released = new([_original, _original], fixtures);
        _current = new([_original], fixtures);
    }

    void Because() => _violations = ReleasedSchemaCompatibility.Check(_released, _current);

    [Fact] void should_reject_the_duplicate_released_row() => _violations.ShouldContain(new Violation($"{_original.TypeId}/{_original.Generation}", "released_schema_duplicate"));
    [Fact] void should_report_only_the_duplicate() => _violations.Count.ShouldEqual(1);
}

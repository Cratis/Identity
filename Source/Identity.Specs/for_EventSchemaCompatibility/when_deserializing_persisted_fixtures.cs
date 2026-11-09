// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Identity.Contracts.Accounts.Events;
using Cratis.Identity.Contracts.Check;
using Cratis.Identity.Revocation;
using Cratis.Json;
using Cratis.Serialization;

namespace Cratis.Identity.for_EventSchemaCompatibility;

public class when_deserializing_persisted_fixtures : Specification
{
    static readonly JsonSerializerOptions _options = CreateOptions();

    public static IEnumerable<TheoryDataRow<Type, string>> PersistedGenerations => Types.Select(type =>
    {
        var shape = ReflectionShapeBuilder.Build(type);
        var schema = SchemaCompatibility.Baseline.Single(row => row.TypeId == shape.TypeId && row.Generation == shape.Generation);
        return new TheoryDataRow<Type, string>(type, schema.Fixture);
    });

    [Theory]
    [MemberData(nameof(PersistedGenerations))]
    public async Task should_deserialize_and_round_trip_every_persisted_generation(Type eventType, string fixture)
    {
        await using var stream = SchemaCompatibility.OpenFixture(fixture);
        stream.ShouldNotBeNull();
        var json = (await JsonNode.ParseAsync(stream, cancellationToken: TestContext.Current.CancellationToken)).AsObject();
        var serializer = new EventSerializer(
            Substitute.For<IClientArtifactsProvider>(),
            Substitute.For<IClientArtifactsActivator>(),
            Substitute.For<IEventTypes>(),
            _options,
            Substitute.For<IDerivedTypes>());

        var result = await serializer.Deserialize(eventType, json);

        eventType.IsInstanceOfType(result).ShouldBeTrue();
        JsonNode.DeepEquals(json, JsonSerializer.SerializeToNode(result, eventType, _options)).ShouldBeTrue();
    }

    [Fact]
    public void should_cover_every_manifest_generation()
    {
        var compiled = Types.Select(type => SchemaCompatibility.Describe(type, ReflectionShapeBuilder.Build(type)));
        SchemaCompatibility.Check(compiled, SchemaCompatibility.Baseline, SchemaCompatibility.FixtureExists).ShouldBeEmpty();
        PersistedGenerations.Count().ShouldEqual(SchemaCompatibility.Baseline.Count);
        SchemaCompatibility.Baseline.Count.ShouldBeGreaterThan(0);
    }

    static IReadOnlyList<Type> Types => ReflectionShapeBuilder.Discover([typeof(AccountRegistered).Assembly, typeof(GrantId).Assembly]);

    static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new EnumerableConceptAsJsonConverterFactory());
        options.Converters.Add(new ConceptAsJsonConverterFactory());
        return options;
    }
}

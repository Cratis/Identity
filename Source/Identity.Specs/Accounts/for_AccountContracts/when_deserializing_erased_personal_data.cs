// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Events;
using Cratis.Concepts;
using Cratis.Identity.Contracts.Accounts.Events;
using Cratis.Json;
using Cratis.Serialization;

namespace Cratis.Identity.Accounts.for_AccountContracts;

public class when_deserializing_erased_personal_data
{
    public static IEnumerable<TheoryDataRow<Type, string>> PersonalEventProperties => typeof(AccountNameChanged).Assembly.GetTypes()
        .Where(type => type.Namespace == typeof(AccountNameChanged).Namespace)
        .SelectMany(type => type.GetProperties()
            .Where(property => property.PropertyType.IsDefined(typeof(PIIAttribute)))
            .Select(property => new TheoryDataRow<Type, string>(type, property.Name)));

    [Theory]
    [MemberData(nameof(PersonalEventProperties))]
    async Task should_read_every_erased_personal_event_property(Type eventType, string propertyName)
    {
        // Chronicle's PII release returns a JSON empty string after the subject key is destroyed.
        // Use its event serializer and the same concept converter factories registered by ChronicleClient.
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new EnumerableConceptAsJsonConverterFactory());
        options.Converters.Add(new ConceptAsJsonConverterFactory());
        var serializer = new EventSerializer(
            Substitute.For<IClientArtifactsProvider>(),
            Substitute.For<IClientArtifactsActivator>(),
            Substitute.For<IEventTypes>(),
            options,
            Substitute.For<IDerivedTypes>());
        var json = new JsonObject { [propertyName] = string.Empty, ["idpIssuer"] = "https://github.com" };

        var result = await serializer.Deserialize(eventType, json);

        eventType.GetProperty(propertyName).GetValue(result).GetConceptValue().ShouldEqual(string.Empty);
    }

    [Fact]
    void should_cover_all_current_personal_event_properties() => PersonalEventProperties.Count().ShouldEqual(9);
}

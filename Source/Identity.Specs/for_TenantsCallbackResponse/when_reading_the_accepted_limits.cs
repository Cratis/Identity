// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Identity.Tenants;

namespace Cratis.Identity.for_TenantsCallbackResponse;

public class when_reading_the_accepted_limits : Specification
{
    SelectableTenant[] _expected;
    IEnumerable<SelectableTenant> _result;

    void Establish() => _expected = Enumerable.Range(0, 256)
        .Select(index => new SelectableTenant($"{index:d3}{new string('a', 61)}", new string('n', 128))).ToArray();

    void Because()
    {
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(new { tenants = _expected.Select(tenant => new { id = tenant.Id, name = tenant.Name }) }));
        _result = TenantsCallbackResponse.Read(document.RootElement);
    }

    [Fact] void should_accept_256_tenants_with_64_character_ids_and_128_character_names_in_order() => _result.ShouldEqual(_expected);
    [Fact] void should_materialize_before_the_document_is_disposed() => _result.ShouldBeOfExactType<List<SelectableTenant>>();
}

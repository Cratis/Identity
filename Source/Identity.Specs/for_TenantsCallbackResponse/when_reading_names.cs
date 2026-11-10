// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Identity.Tenants;

namespace Cratis.Identity.for_TenantsCallbackResponse;

public class when_reading_names : Specification
{
    IEnumerable<SelectableTenant> _result;

    void Because()
    {
        using var document = JsonDocument.Parse("""{"tenants":[{"id":"z","name":""},{"id":"a","name":"   "},{"id":"m"},{"id":"b","name":"  Product name  "}]}""");
        _result = TenantsCallbackResponse.Read(document.RootElement);
    }

    [Fact]
    void should_fall_back_to_the_id_for_empty_whitespace_or_missing_names_without_trimming_or_reordering() => _result.ShouldEqual([
        new SelectableTenant("z", "z"), new SelectableTenant("a", "a"), new SelectableTenant("m", "m"), new SelectableTenant("b", "  Product name  ")
    ]);
}

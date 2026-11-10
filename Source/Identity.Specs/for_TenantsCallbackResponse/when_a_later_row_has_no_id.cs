// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Identity.Tenants;

namespace Cratis.Identity.for_TenantsCallbackResponse;

public class when_a_later_row_has_no_id : Specification
{
    IEnumerable<SelectableTenant> _result;

    void Because()
    {
        using var document = JsonDocument.Parse("""{"tenants":[{"id":"valid"},{"name":"missing id"}]}""");
        _result = TenantsCallbackResponse.Read(document.RootElement);
    }

    [Fact] void should_reject_the_whole_response() => _result.ShouldBeNull();
}

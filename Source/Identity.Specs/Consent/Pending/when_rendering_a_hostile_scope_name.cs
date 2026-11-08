// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.ConsentFlow.Pending;

public class when_rendering_a_hostile_scope_name : given.a_consent_interaction
{
    const string Hostile = "<img/src=x/onerror=alert(1)>";
    PendingConsent _rendered;

    void Because()
    {
        var hostile = request with { Binding = binding with { Scopes = Hostile } };
        _rendered = ConsentPresentation.Create(hostile, available, token);
    }

    [Fact] void should_escape_the_scope_name() => _rendered.Scopes.Single().Name.ShouldEqual("&lt;img/src=x/onerror=alert(1)&gt;");
    [Fact] void should_not_leak_markup_in_the_scope_description() => _rendered.Scopes.Single().Description.ShouldEqual("&lt;img/src=x/onerror=alert(1)&gt;");
}

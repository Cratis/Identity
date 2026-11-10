// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.ConsentFlow.Pending;

public class when_rendering_pending_consent : given.a_consent_interaction
{
    PendingConsent? _signedOut;
    PendingConsent? _otherSession;
    PendingConsent? _disabled;
    ConsentRequest? _after;

    async Task Because()
    {
        context.HttpContext.User = new(new System.Security.Claims.ClaimsIdentity());
        _signedOut = await interaction.Find(CancellationToken.None);
        context.HttpContext.User = new(new System.Security.Claims.ClaimsIdentity([new(System.Security.Claims.ClaimTypes.NameIdentifier, binding.Account.Value)], "spec"));
        sessions.SessionId.Returns("other-session");
        _otherSession = await interaction.Find(CancellationToken.None);
        sessions.SessionId.Returns(binding.Session);
        eligibility.Evaluate(Arg.Any<ConsentRequest>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<ConsentEligibility?>(null));
        _disabled = await interaction.Find(CancellationToken.None);
        _after = await store.Read(request.Id, CancellationToken.None);
    }

    [Fact] void should_not_show_signed_out_requests() => _signedOut.ShouldBeNull();
    [Fact] void should_not_show_another_sessions_request() => _otherSession.ShouldBeNull();
    [Fact] void should_not_show_ineligible_requests() => _disabled.ShouldBeNull();
    [Fact] void should_escape_resource_names() => displayed.Resource.ShouldEqual("Resource &lt;script&gt;name&lt;/script&gt;");
    [Fact] void should_not_expose_internal_bindings() => typeof(PendingConsent).GetProperties().Select(_ => _.Name).ShouldEqual(["Id", "ClientName", "Resource", "Scopes", "Tenants", "ExpiresAt", "Warning", "Proof"]);
    [Fact] void should_escape_scope_descriptions() => displayed.Scopes.Single(_ => _.Name == "read").Description.ShouldEqual("Read &lt;script&gt;permissions&lt;/script&gt;");
    [Fact] void should_escape_tenant_names() => displayed.Tenants.Single().Name.ShouldEqual("Tenant &lt;script&gt;name&lt;/script&gt;");
    [Fact] void should_expose_a_bound_proof() => proof.Validate(displayed.Proof, request).ShouldBeTrue();
    [Fact] void should_warn_the_user() => displayed.Warning.ShouldEqual("Only continue if you started this.");
    [Fact] void should_not_decide_on_get() => _after.ShouldEqual(request);
}

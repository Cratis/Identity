// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.ConsentFlow.Pending.given;

namespace Cratis.Identity.ConsentFlow.Deciding;

public class when_refusing_hostile_decisions : a_consent_interaction
{
    readonly List<Exception?> _refusals = [];
    ConsentRequest? _after;

    async Task Because()
    {
        await Refused(() => new ApproveConsent(request.Id, binding.Tenant, ["openid", "read"], null).Handle(interaction));
        await Refused(() => Deny(submittedProof: string.Empty));
        await Refused(() => Approve(submittedProof: string.Empty));
        await Refused(() => Approve(submittedProof: token + "altered"));
        await Refused(() => Approve(id: ConsentRequestId.New()));
        var other = await activation.Create(binding, CancellationToken.None);
        await Refused(() => Approve(id: other.Id));
        var principal = context.HttpContext.User;
        context.HttpContext.User = new(new System.Security.Claims.ClaimsIdentity([new(System.Security.Claims.ClaimTypes.NameIdentifier, "00000000-0000-4000-8000-000000000099")], "spec"));
        await Refused(() => Approve());
        context.HttpContext.User = principal;
        await Refused(() => Approve(tenant: "unavailable"));
        await Refused(() => Approve(scopes: ["openid", "read", "write"]));
        await Refused(() => Approve(scopes: ["read"]));
        await Refused(() => Approve(scopes: ["openid", "read", "read"]));
        sessions.SessionId.Returns("other-session");
        await Refused(() => Approve());
        sessions.SessionId.Returns((string?)null);
        await Refused(() => Deny());
        sessions.SessionId.Returns(binding.Session);
        eligibility.Evaluate(Arg.Any<ConsentRequest>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<ConsentEligibility?>(null));
        await Refused(() => Approve());
        eligibility.Evaluate(Arg.Any<ConsentRequest>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<ConsentEligibility?>(available));
        clock.Now = request.Deadline;
        await Refused(() => Deny());
        _after = await store.Read(request.Id, CancellationToken.None);
    }

    async Task Refused(Func<Task> action) => _refusals.Add(await Record.ExceptionAsync(action));

    [Fact] void should_refuse_every_hostile_submission() => _refusals.Count.ShouldEqual(15);
    [Fact] void should_use_one_nondisclosing_refusal() => _refusals.TrueForAll(_ => _ is ConsentDecisionRefused && _.Message == "consent_decision_refused").ShouldBeTrue();
    [Fact] void should_leave_the_request_unchanged() => _after.ShouldEqual(request);
    [Fact] void should_never_write_evidence() => evidence.Appends.ShouldEqual(0);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Authorization;
using Cratis.Identity.Configuration;
using Cratis.Identity.Resources;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.ConsentFlow.Pending.given;

public class a_consent_interaction : Deciding.given.an_activation
{
    internal readonly IHttpContextAccessor context = Substitute.For<IHttpContextAccessor>();
    internal readonly ICurrentConsentSession sessions = Substitute.For<ICurrentConsentSession>();
    internal readonly IConsentEligibility eligibility = Substitute.For<IConsentEligibility>();
    internal IConsentProof proof;
    internal ConsentInteraction interaction;
    internal PendingConsent? displayed;
    internal string token;
    internal ConsentEligibility available;

    async Task Establish()
    {
        context.HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, binding.Account.Value)], "spec"))
        };
        sessions.SessionId.Returns(binding.Session);
        available = new(new Grant(new ResourceServer(binding.Resource, "Resource <script>name</script>", [binding.Resource], [new("read", "Read <script>permissions</script>")]), ["openid", "read"]), [new(binding.Tenant, "Tenant <script>name</script>")]);
        eligibility.Evaluate(Arg.Any<ConsentRequest>(), Arg.Any<CancellationToken>()).Returns(call => Task.FromResult<ConsentEligibility?>(available));
        proof = new ConsentProof(new EphemeralDataProtectionProvider(), clock);
        interaction = new(store, new ConsentCaller(context, sessions, Options.Create(new IdentityOptions { Issuer = binding.Realm })), eligibility, proof, clock, Options.Create(options));
        token = proof.Issue(request);
        displayed = await interaction.Find(CancellationToken.None);
    }

    internal Task Approve(ConsentRequestId? id = null, string? tenant = null, string[]? scopes = null, string? submittedProof = null) =>
        new ApproveConsent(id ?? request.Id, tenant ?? binding.Tenant, scopes ?? ["read", "openid"], submittedProof ?? token).Handle(interaction);

    internal Task Deny(string? submittedProof = null) => new DenyConsent(request.Id, submittedProof ?? token).Handle(interaction);
}

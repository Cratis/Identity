// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cratis.Identity.for_IdentityOptionsValidation;

public class when_reading_oidc_defaults : Specification
{
    IdentityOptions _defaults;
    IdentityOptions _bound;
    OidcClientOptions _client;

    void Because()
    {
        _defaults = new();
        _client = new();
        _bound = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Identity:Oidc:IssuanceEnabled"] = "true",
            ["Identity:Oidc:ConsentDecision"] = "ImplicitFencedPath",
            ["Identity:OidcClients:0:ClientId"] = "oidc-web",
            ["Identity:OidcClients:0:Scopes:0"] = "openid",
            ["Identity:OidcClients:1:ClientId"] = "oidc-default"
        }).Build().GetSection(IdentityOptions.SectionName).Get<IdentityOptions>();
    }

    [Fact] void should_have_no_oidc_clients() => _defaults.OidcClients.ShouldBeEmpty();
    [Fact] void should_keep_issuance_closed() => _defaults.Oidc.IssuanceEnabled.ShouldBeFalse();
    [Fact] void should_have_no_recorded_consent_decision() => _defaults.Oidc.ConsentDecision.ShouldEqual(string.Empty);
    [Fact] void should_leave_the_configured_list_omitted() => _client.Scopes.ShouldBeNull();
    [Fact] void should_default_the_effective_ceiling_to_openid_and_profile() => _client.EffectiveScopes.ShouldContainOnly("openid", "profile");
    [Fact] void should_bind_the_gate() => _bound.Oidc.IssuanceEnabled.ShouldBeTrue();
    [Fact] void should_bind_the_decision() => _bound.Oidc.ConsentDecision.ShouldEqual("ImplicitFencedPath");
    [Fact] void should_bind_clients() => _bound.OidcClients.Select(_ => _.ClientId).ShouldContainOnly("oidc-web", "oidc-default");
    [Fact] void should_bind_exactly_the_explicit_openid_only_list() => _bound.OidcClients[0].Scopes.ShouldContainOnly("openid");
    [Fact] void should_not_widen_an_explicit_openid_only_ceiling() => _bound.OidcClients[0].EffectiveScopes.ShouldContainOnly("openid");
    [Fact] void should_keep_the_default_for_an_omitted_configured_list() => _bound.OidcClients[1].EffectiveScopes.ShouldContainOnly("openid", "profile");
}

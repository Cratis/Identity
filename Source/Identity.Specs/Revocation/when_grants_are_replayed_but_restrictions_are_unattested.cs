// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;

namespace Cratis.Identity.RevocationSpecs;

public class when_grants_are_replayed_but_restrictions_are_unattested : given.a_recovery_pass
{
    async Task Because()
    {
        _custody = _custody with { Epoch = 1, ClearedEpoch = -1 };
        await Run();
    }

    [Fact] void should_complete_replay() => _failure.ShouldBeNull();
    [Fact] void should_cover_tokens() => _gate.Allows(RestrictionClass.Tokens).ShouldBeTrue();
    [Fact] void should_cover_authorizations() => _gate.Allows(RestrictionClass.Authorizations).ShouldBeTrue();
    [Fact] void should_cover_consent() => _gate.Allows(RestrictionClass.Consent).ShouldBeTrue();
    [Fact] void should_cover_keys() => _gate.Allows(RestrictionClass.Keys).ShouldBeTrue();
    [Fact] void should_cover_configured_client_secrets() => _gate.Allows(RestrictionClass.ClientSecrets).ShouldBeTrue();
    [Fact] void should_not_cover_accounts() => _gate.Allows(RestrictionClass.Accounts).ShouldBeFalse();
    [Fact] void should_not_cover_browser_sessions() => _gate.Allows(RestrictionClass.BrowserSessions).ShouldBeFalse();
    [Fact] void should_not_cover_on_behalf_sessions() => _gate.Allows(RestrictionClass.OnBehalfSessions).ShouldBeFalse();
    [Fact] void should_not_cover_runner_sessions() => _gate.Allows(RestrictionClass.RunnerSessions).ShouldBeFalse();
    [Fact] void should_not_cover_delegation() => _gate.Allows(RestrictionClass.Delegation).ShouldBeFalse();
    [Fact] void should_not_cover_personal_access_tokens() => _gate.Allows(RestrictionClass.PersonalAccessTokens).ShouldBeFalse();
    [Fact] void should_still_require_global_attestation() => _gate.Ready.ShouldBeFalse();
    [Fact] void should_keep_legacy_bootstrap_disabled() => _gate.AllowLegacyBootstrap.ShouldBeFalse();
}

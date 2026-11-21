// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Security.Claims;
using System.Text.Json;
using Cratis.Identity.Accounts;
using Cratis.Identity.Authorization;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Device;
using Cratis.Identity.Revocation;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.for_DeviceGrantAdmission.given;

public class an_approved_device : Specification
{
    private protected RecoveryGate _gate;
    private protected IIndependentFenceLog _log;
    private protected IOpenIddictTokenManager _tokens;
    private protected IOpenIddictAuthorizationManager _authorizations;
    private protected IFenceStore _fences;
    private protected IAccountReader _accounts;
    private protected DeviceGrantAdmission _admission;
    protected ClaimsPrincipal _principal;
    protected object _token;
    protected object _authorization;
    protected bool _accepted;
    protected bool _baseline;

    async Task Establish()
    {
        _gate = new();
        _gate.ObserveEpoch(7);
        _gate.Open(true);
        _log = Substitute.For<IIndependentFenceLog>();
        _log.RecoveryEpoch(Arg.Any<CancellationToken>()).Returns(7L);
        _tokens = Substitute.For<IOpenIddictTokenManager>();
        _authorizations = Substitute.For<IOpenIddictAuthorizationManager>();
        _fences = Substitute.For<IFenceStore>();
        _accounts = Substitute.For<IAccountReader>();
        _token = new();
        _authorization = new();
        _principal = new ClaimsPrincipal(new ClaimsIdentity("test"))
            .SetTokenId("device-entry").SetAuthorizationId("authorization")
            .SetClaim(Claims.Subject, "0195d1c1-4f00-4000-8000-000000000001");
        _tokens.FindByIdAsync("device-entry", Arg.Any<CancellationToken>()).Returns(_token);
        _tokens.GetPropertiesAsync(_token, Arg.Any<CancellationToken>()).Returns(Stamp(7));
        _authorizations.FindByIdAsync("authorization", Arg.Any<CancellationToken>()).Returns(_authorization);
        _authorizations.HasStatusAsync(_authorization, Statuses.Valid, Arg.Any<CancellationToken>()).Returns(true);
        _authorizations.GetPropertiesAsync(_authorization, Arg.Any<CancellationToken>()).Returns(Stamp(7));
        _authorizations.GetSubjectAsync(_authorization, Arg.Any<CancellationToken>()).Returns(_principal.GetClaim(Claims.Subject));
        _accounts.FindActive(Arg.Any<AccountSubject>(), Arg.Any<CancellationToken>()).Returns(Identity.given.GitHubAccounts.Create(new(_principal.GetClaim(Claims.Subject)), "Member", null, "42", "member"));
        _admission = new(_gate, _log, _tokens, _authorizations, _fences, _accounts);
        _baseline = await _admission.CanUse(_principal, true, CancellationToken.None);
    }

    protected static ImmutableDictionary<string, JsonElement> Stamp(long epoch) => ImmutableDictionary<string, JsonElement>.Empty.Add(RecoveryEpochStamp.PropertyName, JsonSerializer.SerializeToElement(epoch));
}

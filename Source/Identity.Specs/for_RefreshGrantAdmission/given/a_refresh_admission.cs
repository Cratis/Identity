// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Accounts;
using Cratis.Identity.Authorization;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Revocation;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.for_RefreshGrantAdmission.given;

public class a_refresh_admission : Specification
{
    internal const string GrantId = "grant-1";
    internal static readonly Guid GitHubLink = Guid.Parse("11111111-1111-4111-8111-111111111111");
    internal static readonly Guid DirectoryLink = Guid.Parse("22222222-2222-4222-8222-222222222222");

    internal IFenceStore _fences;
    internal RefreshGrantAdmission _admission;
    internal ClaimsPrincipal _granted;
    internal OpenIddictRequest _request;
    internal Account _account;
    internal bool _result;

    void Establish()
    {
        _fences = Substitute.For<IFenceStore>();
        _admission = new(_fences);
        _granted = new(new ClaimsIdentity());
        _granted.SetAuthorizationId(GrantId);
        _request = new() { GrantType = GrantTypes.RefreshToken };
        _account = AccountWithLinks(DirectoryLink);
    }

    internal static Account AccountWithLinks(params Guid[] links) =>
        new(
            new("7d9d5c9e-0000-4000-8000-000000000001"),
            "Member",
            null,
            AccountStatus.Active,
            links.Select(link => new LinkedUpstreamKey(link, new ConnectionId(Guid.NewGuid()), "https://login.example", Guid.NewGuid().ToString(), "member", DateTimeOffset.UnixEpoch)).ToArray());

    internal void StateLinkedTo(Guid? link) => StateOf(GrantId, link);

    internal void StateOf(string grantId, Guid? link)
    {
        var state = new RevocationSpecs.SharedFenceStore()
            .RecordFamilyStart(new FamilyStart(grantId, "entry", "7d9d5c9e-0000-4000-8000-000000000001", DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddDays(30), link), CancellationToken.None)
            .GetAwaiter()
            .GetResult();
        _fences.Read(grantId, Arg.Any<CancellationToken>()).Returns(state);
    }
}

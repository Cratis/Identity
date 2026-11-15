// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;

namespace Cratis.Identity.for_RefreshGrantAdmission;

public class when_the_grant_recorded_no_link : given.a_refresh_admission
{
    void Establish() => StateLinkedTo(null);

    async Task Because() => _result = await _admission.IsValid(_granted, _request, _account, CancellationToken.None);

    [Fact] void should_refuse_the_refresh() => _result.ShouldBeFalse();
    [Fact] void should_fence_the_family() => _fences.Received(1).Commit(GrantId, _account.Id.Value, RevocationReason.Revoked, Arg.Any<CancellationToken>());
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;

namespace Cratis.Identity.for_RefreshGrantAdmission;

public class when_the_authenticating_link_is_still_linked : given.a_refresh_admission
{
    void Establish() => StateLinkedTo(DirectoryLink);

    async Task Because() => _result = await _admission.IsValid(_granted, _request, _account, CancellationToken.None);

    [Fact] void should_admit_the_refresh() => _result.ShouldBeTrue();
    [Fact] void should_not_fence_the_family() => _fences.DidNotReceive().Commit(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<RevocationReason>(), Arg.Any<CancellationToken>());
}

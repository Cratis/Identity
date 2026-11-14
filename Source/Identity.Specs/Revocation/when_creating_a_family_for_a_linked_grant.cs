// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;

namespace Cratis.Identity.RevocationSpecs;

public class when_creating_a_family_for_a_linked_grant : given.a_token_store
{
    static readonly Guid _link = Guid.Parse("33333333-3333-4333-8333-333333333333");
    GrantState _state;

    async Task Because()
    {
        _issuance.StartLink = _link;
        await _store.CreateAsync(_entry, CancellationToken.None);
        _state = await _fences.Read(_entry.AuthorizationId.ToString(), CancellationToken.None);
    }

    [Fact] void should_record_the_authenticating_link_on_the_family() => _state.LinkId.ShouldEqual(_link);
}

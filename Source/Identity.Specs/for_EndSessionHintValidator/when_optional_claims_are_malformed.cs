// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_EndSessionHintValidator;

public class when_optional_claims_are_malformed : given.a_hint_validator
{
    bool _nullParty;
    bool _objectParty;
    bool _arrayParty;
    bool _nullType;
    bool _objectType;
    bool _nullScope;
    bool _nullClient;
    bool _nullNotBefore;
    bool _absent;
    bool _valid;

    async Task Because()
    {
        _nullParty = await Refused(Sign(_ => _["azp"] = null));
        _objectParty = await Refused(Sign(_ => _["azp"] = new { client = ClientId }));
        _arrayParty = await Refused(Sign(_ => _["azp"] = new[] { ClientId }));
        _nullType = await Refused(Sign(_ => _["oi_tkn_typ"] = null));
        _objectType = await Refused(Sign(_ => _["oi_tkn_typ"] = new { type = "id_token" }));
        _nullScope = await Refused(Sign(_ => _["scope"] = null));
        _nullClient = await Refused(Sign(_ => _["cid"] = null));
        _nullNotBefore = await Refused(Sign(_ => _["nbf"] = null));
        _absent = await Refused(Sign());
        _valid = await Refused(Sign(_ => { _["azp"] = ClientId; _["oi_tkn_typ"] = "id_token"; }));
    }

    [Fact] void should_reject_a_null_authorized_party() => _nullParty.ShouldBeTrue();
    [Fact] void should_reject_an_object_authorized_party() => _objectParty.ShouldBeTrue();
    [Fact] void should_reject_an_array_authorized_party() => _arrayParty.ShouldBeTrue();
    [Fact] void should_reject_a_null_private_token_type() => _nullType.ShouldBeTrue();
    [Fact] void should_reject_an_object_private_token_type() => _objectType.ShouldBeTrue();
    [Fact] void should_reject_even_a_null_access_token_scope_marker() => _nullScope.ShouldBeTrue();
    [Fact] void should_reject_even_a_null_access_token_client_marker() => _nullClient.ShouldBeTrue();
    [Fact] void should_reject_a_present_but_null_not_before_time() => _nullNotBefore.ShouldBeTrue();
    [Fact] void should_accept_absent_optional_claims() => _absent.ShouldBeFalse();
    [Fact] void should_accept_matching_optional_claims() => _valid.ShouldBeFalse();
}

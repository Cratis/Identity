// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization.TokenExchange;
using Microsoft.IdentityModel.Tokens;

namespace Cratis.Identity.for_SubjectTokenValidator;

public class when_the_issuer_keys_are_unavailable : given.a_subject_token_validator
{
    SubjectTokenResult _result;

    void Establish() => keys.Get(default!, default, default).ReturnsForAnyArgs(Task.FromResult<IReadOnlyCollection<SecurityKey>?>(null));

    async Task Because() => _result = await Validate(Valid());

    [Fact] void should_refuse_as_keys_unavailable() => _result.Refusal.ShouldEqual(SubjectTokenRefusal.KeysUnavailable);
    [Fact] void should_not_accept_anything() => _result.Subject.ShouldBeNull();
}

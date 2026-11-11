// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.for_EndSessionHintValidator;

public class when_enforcing_client_permissions : given.a_hint_validator
{
    [Theory]
    [InlineData(false, false, true)]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, true, false)]
    public async Task should_require_both_openid_and_end_session(bool openid, bool endSession, bool refused)
    {
        var permissions = ImmutableArray.CreateBuilder<string>();
        if (openid)
        {
            permissions.Add(Permissions.Prefixes.Scope + Scopes.OpenId);
        }

        if (endSession)
        {
            permissions.Add(Permissions.Endpoints.EndSession);
        }

        applications.GetPermissionsAsync(application, Arg.Any<CancellationToken>()).Returns(new ValueTask<ImmutableArray<string>>(permissions.ToImmutable()));
        (await Refused(Sign())).ShouldEqual(refused);
    }
}

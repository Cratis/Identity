// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_EndSessionHintValidator;

public class when_the_server_issuer_is_missing_or_different : given.a_hint_validator
{
    [Theory]
    [InlineData(null)]
    [InlineData("https://other-issuer.example/")]
    public async Task should_refuse_the_hint(string? issuer)
    {
        server.Issuer = issuer is null ? null : new Uri(issuer);
        (await Refused(Sign())).ShouldBeTrue();
    }
}

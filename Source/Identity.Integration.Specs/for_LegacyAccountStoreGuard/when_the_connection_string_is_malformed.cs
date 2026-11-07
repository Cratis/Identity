// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Operational;

namespace Cratis.Identity.Integration.for_LegacyAccountStoreGuard;

[Collection(IdentityCollection.Name)]
public class when_the_connection_string_is_malformed(IdentityHost host)
{
    const string Malformed = "mongodb://marker-user:marker-pass@marker-host:not-a-port/?marker=1";

    [Fact]
    public async Task should_refuse_to_start_without_leaking_the_connection_string()
    {
        var scenario = new given.LegacyGuardScenario(host);

        var error = await scenario.Start(Malformed);

        error.ShouldBeOfExactType<LegacyAccountStoreUnverifiable>();
        error.Message.ShouldContain("(error)");
        error.Message.ShouldNotContain("marker");
        error.InnerException.ShouldBeNull();
    }
}

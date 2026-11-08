// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.for_LegacyAccountStoreGuard;

[Collection(IdentityCollection.Name)]
public class when_no_legacy_account_exists(IdentityHost host)
{
    [Fact]
    public async Task should_start_without_creating_anything_when_the_collection_is_absent()
    {
        var scenario = new given.LegacyGuardScenario(host);

        (await scenario.Start()).ShouldBeNull();
        (await scenario.CollectionNames(scenario.Configured)).ShouldBeEmpty();
    }

    [Fact]
    public async Task should_start_when_the_collection_is_empty()
    {
        var scenario = new given.LegacyGuardScenario(host);
        await scenario.Configured.CreateCollectionAsync("accounts", cancellationToken: TestContext.Current.CancellationToken);

        (await scenario.Start()).ShouldBeNull();
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Operational;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Identity.Integration.for_LegacyAccountStoreGuard;

[Collection(IdentityCollection.Name)]
public class when_a_legacy_account_exists(IdentityHost host)
{
    [Fact]
    public async Task should_refuse_to_start_and_change_nothing()
    {
        var scenario = new given.LegacyGuardScenario(host);
        await scenario.InsertLegacyAccount(scenario.Configured);

        var error = await scenario.Start();

        error.ShouldBeOfExactType<LegacyAccountStoreFound>();
        (await scenario.Configured.GetCollection<BsonDocument>("accounts").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty, cancellationToken: TestContext.Current.CancellationToken)).ShouldEqual(1L);
        (await scenario.CollectionNames(scenario.Configured)).ShouldContainOnly("accounts");
        (await scenario.CollectionNames(new MongoClient(scenario.ConnectionString).GetDatabase(scenario.DefaultDatabase))).ShouldBeEmpty();
    }

    [Fact]
    public async Task should_start_when_the_operator_confirmed()
    {
        var scenario = new given.LegacyGuardScenario(host);
        await scenario.InsertLegacyAccount(scenario.Configured);

        (await scenario.Start(optOut: true)).ShouldBeNull();
    }
}

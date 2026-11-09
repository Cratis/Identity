// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;
using Cratis.Identity.Revocation.Operational;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Identity.Integration.for_GrantFences;

[Collection(IdentityCollection.Name)]
public class when_the_host_provisions_independent_custody(IdentityHost host) : IAsyncLifetime
{
    bool _differentDatabase;
    bool _differentDeployment;
    bool _matchingInstance;
    Exception? _failure;

    public async ValueTask InitializeAsync()
    {
        var connection = host.Services.GetRequiredService<IndependentFenceLogConnection>();
        var options = host.Services.GetRequiredService<FenceLogOptions>();
        _failure = await Catch.Exception(() => connection.Verify(CancellationToken.None));
        _differentDatabase = connection.Database.DatabaseNamespace.DatabaseName != host.Database.DatabaseNamespace.DatabaseName;
        var operational = await host.Database.Client.GetDatabase("admin").RunCommandAsync<BsonDocument>(new BsonDocument("replSetGetConfig", 1));
        var independent = await connection.Database.Client.GetDatabase("admin").RunCommandAsync<BsonDocument>(new BsonDocument("replSetGetConfig", 1));
        _differentDeployment = operational["config"]["settings"]["replicaSetId"] != independent["config"]["settings"]["replicaSetId"];
        var metadata = await connection.Database.GetCollection<BsonDocument>("fence-custody").Find(new BsonDocument("_id", "metadata")).SingleAsync();
        _matchingInstance = metadata["instanceId"].AsString == options.InstanceId;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    [Fact] void should_use_a_distinct_database() => _differentDatabase.ShouldBeTrue();
    [Fact] void should_use_a_distinct_replica_set_identity() => _differentDeployment.ShouldBeTrue();
    [Fact] void should_provision_the_configured_custody_identity() => _matchingInstance.ShouldBeTrue();
    [Fact] void should_pass_the_unchanged_production_boundary_check() => _failure.ShouldBeNull();
}

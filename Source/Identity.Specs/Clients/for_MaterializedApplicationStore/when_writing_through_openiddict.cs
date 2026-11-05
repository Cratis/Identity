// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Storage;
using Microsoft.Extensions.Options;
using OpenIddict.MongoDb;
using OpenIddict.MongoDb.Models;

namespace Cratis.Identity.Clients.for_MaterializedApplicationStore;

public class when_writing_through_openiddict
{
    readonly MaterializedApplicationStore<OpenIddictMongoDbApplication> _store = new(Substitute.For<IOpenIddictMongoDbContext>(), Substitute.For<IOptionsMonitor<OpenIddictMongoDbOptions>>());

    [Fact] void should_refuse_create() => Refused(() => _store.CreateAsync(new(), CancellationToken.None));
    [Fact] void should_refuse_update() => Refused(() => _store.UpdateAsync(new(), CancellationToken.None));
    [Fact] void should_refuse_delete() => Refused(() => _store.DeleteAsync(new(), CancellationToken.None));

    static void Refused(Func<ValueTask> write)
    {
        var error = Catch.Exception(() => _ = write());
        error.ShouldBeOfExactType<InvalidOperationException>();
        error.Message.ShouldEqual("client configuration is written by Chronicle facts");
    }
}

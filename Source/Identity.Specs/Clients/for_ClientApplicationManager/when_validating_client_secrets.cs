// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Clients.Secrets;
using Cratis.Identity.Contracts.Clients;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Core;
using OpenIddict.MongoDb.Models;

namespace Cratis.Identity.Clients.for_ClientApplicationManager;

public class when_validating_client_secrets : Specification
{
    ClientApplicationManager<OpenIddictMongoDbApplication> _manager;
    OpenIddictMongoDbApplication _application;
    IOpenIddictApplicationStore<OpenIddictMongoDbApplication> _store;

    void Establish()
    {
        _application = new();
        _store = Substitute.For<IOpenIddictApplicationStore<OpenIddictMongoDbApplication>>();
        _store.GetClientTypeAsync(_application, Arg.Any<CancellationToken>()).Returns(OpenIddictConstants.ClientTypes.Confidential);
        _store.GetClientIdAsync(_application, Arg.Any<CancellationToken>()).Returns("test-client");
        var digester = new ClientSecretDigester(given.client_test_keys.Create());
        var comparand = digester.Digest(ClientId.Create("test-client"), "current").Value;
        _store.GetClientSecretAsync(_application, Arg.Any<CancellationToken>()).Returns(comparand);
        _manager = new(
            Substitute.For<IOpenIddictApplicationCache<OpenIddictMongoDbApplication>>(),
            NullLogger<OpenIddictApplicationManager<OpenIddictMongoDbApplication>>.Instance,
            Substitute.For<IOptionsMonitor<OpenIddictCoreOptions>>(),
            _store,
            digester);
    }

    [Fact] async Task should_accept_the_current_secret() => (await _manager.ValidateClientSecretAsync(_application, "current", TestContext.Current.CancellationToken)).ShouldBeTrue();
    [Fact] async Task should_refuse_the_wrong_secret() => (await _manager.ValidateClientSecretAsync(_application, "wrong", TestContext.Current.CancellationToken)).ShouldBeFalse();
    [Fact] async Task should_refuse_the_previous_secret() => (await _manager.ValidateClientSecretAsync(_application, "previous", TestContext.Current.CancellationToken)).ShouldBeFalse();

    [Fact]
    async Task should_refuse_a_legacy_pbkdf2_comparand()
    {
        _store.GetClientSecretAsync(_application, TestContext.Current.CancellationToken).Returns("AQAAAAIAAYagAAAAEFakeLegacyPBKDF2Comparand");
        (await _manager.ValidateClientSecretAsync(_application, "current", TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    [Fact]
    async Task should_refuse_a_public_client()
    {
        _store.GetClientTypeAsync(_application, TestContext.Current.CancellationToken).Returns(OpenIddictConstants.ClientTypes.Public);
        (await _manager.ValidateClientSecretAsync(_application, "current", TestContext.Current.CancellationToken)).ShouldBeFalse();
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Integration.for_Host.given;
using MongoDB.Driver;

namespace Cratis.Identity.Integration.for_Host.for_CratisIdImageFixture;

public class when_moving_a_connection_string_into_the_network : Specification
{
    string _connectionString;
    MongoUrl _result;

    void Establish() => _connectionString = new MongoUrlBuilder
    {
        Server = new MongoServerAddress("localhost", 49123),
        Username = "synthetic-user",
        Password = "synthetic-password",
        AuthenticationSource = "synthetic-auth",
        DirectConnection = false
    }.ToMongoUrl().Url;

    void Because() => _result = new MongoUrl(CratisIdImageFixture.InNetwork(_connectionString, "network-mongo"));

    [Fact] void should_preserve_the_username() => _result.Username.ShouldEqual("synthetic-user");
    [Fact] void should_preserve_the_password() => _result.Password.ShouldEqual("synthetic-password");
    [Fact] void should_preserve_the_authentication_source() => _result.AuthenticationSource.ShouldEqual("synthetic-auth");
    [Fact] void should_use_the_network_alias() => _result.Server.Host.ShouldEqual("network-mongo");
    [Fact] void should_use_the_internal_mongo_port() => _result.Server.Port.ShouldEqual(27017);
    [Fact] void should_connect_directly() => _result.DirectConnection.ShouldEqual(true);
}

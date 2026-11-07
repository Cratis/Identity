// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Hosting;

namespace Cratis.Identity.for_MongoClientFactory;

public class when_the_connection_string_is_malformed : Specification
{
    const string Marker = "canary-marker-9f3a";
    const string Property = "TOKEN:canary-token-7c1d";

    static readonly string[] _malformed =
    [
        $"mongodb://admin:{Marker}@db.example:notaport/?authMechanism=MONGODB-OIDC&authMechanismProperties={Property}",
        $"mongodb://admin:{Marker}@db.example/?maxPoolSize=notanumber&authMechanismProperties={Property}",
        $"notmongo://admin:{Marker}@db.example/?authMechanismProperties={Property}"
    ];

    List<Exception> _exceptions = [];

    void Because() => _exceptions = [.. _malformed.Select(_ => Catch.Exception(() => MongoClientFactory.Create(_))!)];

    [Fact] void should_refuse_every_malformed_string() => _exceptions.TrueForAll(_ => _ is InvalidMongoDBConnectionString).ShouldBeTrue();
    [Fact] void should_name_the_setting() => _exceptions.TrueForAll(_ => _.Message.Contains("Identity:MongoDB:ConnectionString")).ShouldBeTrue();
    [Fact] void should_not_leak_the_credential_in_the_message() => _exceptions.Exists(_ => _.Message.Contains(Marker)).ShouldBeFalse();
    [Fact] void should_not_leak_the_options_in_the_message() => _exceptions.Exists(_ => _.Message.Contains(Property)).ShouldBeFalse();
    [Fact] void should_not_quote_any_part_of_the_string() => _exceptions.Exists(_ => _.Message.Contains("db.example")).ShouldBeFalse();
    [Fact] void should_carry_no_inner_exception() => _exceptions.TrueForAll(_ => _.InnerException is null).ShouldBeTrue();
    [Fact] void should_not_leak_anything_in_the_logged_form() => _exceptions.Exists(_ => _.ToString().Contains(Marker) || _.ToString().Contains(Property)).ShouldBeFalse();
}

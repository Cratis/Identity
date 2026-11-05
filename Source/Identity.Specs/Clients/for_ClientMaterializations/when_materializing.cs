// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using MongoDB.Bson;

namespace Cratis.Identity.Clients.for_ClientMaterializations;

public class when_materializing : given.a_materialization
{
    async Task Because() => await _sink.Upsert(_state, _policy, CancellationToken.None);

    [Fact] void should_use_a_deterministic_object_id() => _document.Id.ToString().ShouldEqual("00112233445546778899aabb");
    [Fact] void should_mark_the_fact_stream() => _document.Properties!["cratis_client_registration_id"].AsString.ShouldEqual(_state.Id);
    [Fact] void should_use_the_fact_client_id() => _document.ClientId.ShouldEqual(_state.ClientId!.Value);
    [Fact] void should_use_fact_redirects() => _document.RedirectUris!.Single().ShouldEqual("https://fact.example/callback");
    [Fact] void should_use_only_the_approved_comparand() => _document.ClientSecret.ShouldEqual(_state.CurrentSecretHash!.Value);
    [Fact] void should_take_permissions_from_policy() => _document.Permissions.ShouldEqual(_policy.Permissions);
    [Fact] void should_take_requirements_from_policy() => _document.Requirements.ShouldEqual(_policy.Requirements);
    [Fact] void should_be_an_upsert() => _replaceOptions.IsUpsert.ShouldBeTrue();
    [Fact] void should_filter_the_upsert_by_id() => _replaced["_id"].AsObjectId.ShouldEqual(_document.Id);
    [Fact] void should_not_serialize_the_raw_secret() => _document.ToJson().Contains(_policy.ClientSecret!, StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_serialize_the_encoded_secret() => _document.ToJson().Contains(Convert.ToBase64String(Encoding.UTF8.GetBytes(_policy.ClientSecret!)), StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_serialize_an_unkeyed_hash() => _document.ToJson().Contains(Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(_policy.ClientSecret!))), StringComparison.Ordinal).ShouldBeFalse();
}

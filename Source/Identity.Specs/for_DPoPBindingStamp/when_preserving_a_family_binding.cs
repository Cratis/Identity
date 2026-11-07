// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Identity.DPoP.Operational;
using Cratis.Identity.Revocation;
using MongoDB.Bson;

namespace Cratis.Identity.for_DPoPBindingStamp;

public class when_preserving_a_family_binding : Specification
{
    [Fact]
    void should_stamp_only_at_creation() => DPoPBindingStamp.Create(null, "key")[DPoPBindingStamp.PropertyName].AsString.ShouldEqual("key");

    [Fact]
    void should_keep_unbound_entries_unbound() => DPoPBindingStamp.Create(null, null).Contains(DPoPBindingStamp.PropertyName).ShouldBeFalse();

    [Fact]
    void should_preserve_a_binding_when_the_manager_omits_it()
    {
        var properties = DPoPBindingStamp.Preserve(DPoPBindingStamp.Create(null, "key"), []);
        properties[DPoPBindingStamp.PropertyName].GetString().ShouldEqual("key");
    }

    [Fact]
    void should_refuse_rebinding() => Catch.Exception(() => DPoPBindingStamp.Preserve(DPoPBindingStamp.Create(null, "key"), ImmutableDictionary<string, JsonElement>.Empty.Add(DPoPBindingStamp.PropertyName, JsonSerializer.SerializeToElement("other")))).ShouldBeOfExactType<RevocationPending>();

    [Fact]
    void should_refuse_late_binding() => Catch.Exception(() => DPoPBindingStamp.Preserve(null, ImmutableDictionary<string, JsonElement>.Empty.Add(DPoPBindingStamp.PropertyName, JsonSerializer.SerializeToElement("key")))).ShouldBeOfExactType<RevocationPending>();

    [Fact]
    void should_refuse_removing_a_binding() => Catch.Exception(() => DPoPBindingStamp.DemandUnchanged(DPoPBindingStamp.Create(null, "key"), new BsonDocument())).ShouldBeOfExactType<RevocationPending>();

    [Fact]
    void should_refuse_adding_a_binding_during_update() => Catch.Exception(() => DPoPBindingStamp.DemandUnchanged(null, DPoPBindingStamp.Create(null, "key"))).ShouldBeOfExactType<RevocationPending>();
}

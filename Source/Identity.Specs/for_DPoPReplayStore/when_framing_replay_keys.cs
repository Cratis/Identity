// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.DPoP.Operational;

namespace Cratis.Identity.for_DPoPReplayStore;

public class when_framing_replay_keys : Specification
{
    [Fact] void should_separate_authorization_and_resource_domains() => MongoDPoPReplayStore.Key("as", "origin", "key", "id").ShouldNotEqual(MongoDPoPReplayStore.Key("rs", "origin", "key", "id"));
    [Fact] void should_separate_origins() => MongoDPoPReplayStore.Key("as", "one", "key", "id").ShouldNotEqual(MongoDPoPReplayStore.Key("as", "two", "key", "id"));
    [Fact] void should_separate_keys() => MongoDPoPReplayStore.Key("as", "origin", "one", "id").ShouldNotEqual(MongoDPoPReplayStore.Key("as", "origin", "two", "id"));
    [Fact] void should_frame_fields_unambiguously() => MongoDPoPReplayStore.Key("as", "ab", "c", "id").ShouldNotEqual(MongoDPoPReplayStore.Key("as", "a", "bc", "id"));
    [Fact] void should_not_store_the_proof_identifier() => MongoDPoPReplayStore.Key("as", "origin", "key", "synthetic-identifier").Contains("synthetic", StringComparison.Ordinal).ShouldBeFalse();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cratis.Identity.for_VectorArtifacts;

public class when_rejecting_changes_without_a_version_bump : given.a_versioned_baseline
{
    JsonNode[] _mutations;
    bool[] _matches;

    void Establish()
    {
        _mutations = Enumerable.Range(0, 4).Select(_ => JsonNode.Parse(_manifest.GetRawText()).AsObject()).ToArray();
        var cases = _mutations[0]["cases"];
        var firstId = cases[0]["id"].GetValue<string>();
        cases[0]["id"] = cases[1]["id"].GetValue<string>();
        cases[1]["id"] = firstId;
        _mutations[1]["cases"][0]["id"] = "fresh-kebab-case-id";
        _mutations[2]["cases"][0]["sha256"] = new string('0', 64);
        _mutations[2]["files"][0]["sha256"] = new string('1', 64);
        _mutations[3]["files"][0]["sha256"] = new string('2', 64);
    }

    void Because() => _matches = _mutations.Select(item => MatchesBaseline(JsonSerializer.SerializeToElement(item))).ToArray();

    [Fact] void should_reject_swapped_ids() => _matches[0].ShouldBeFalse();
    [Fact] void should_reject_a_fresh_id() => _matches[1].ShouldBeFalse();
    [Fact] void should_reject_coordinated_case_and_file_hash_edits() => _matches[2].ShouldBeFalse();
    [Fact] void should_reject_a_file_hash_edit_alone() => _matches[3].ShouldBeFalse();
}

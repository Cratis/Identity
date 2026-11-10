// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Identity.for_VectorArtifacts;

public partial class when_checking_the_manifest_case_ids : given.a_versioned_baseline
{
    string[] _ids;
    int[] _indexes;
    bool _caseHashesMatch;
    bool _baselineMatches;

    [GeneratedRegex("^[a-z0-9][a-z0-9-]*$", RegexOptions.None, 1000)]
    private static partial Regex CaseId { get; }

    void Because()
    {
        _ids = _cases.Select(item => item.GetProperty("id").GetString() ?? string.Empty).ToArray();
        _indexes = _cases.Select(item => item.GetProperty("index").GetInt32()).Order().ToArray();
        _caseHashesMatch = _cases.All(item => CaseHash(_vectors[item.GetProperty("index").GetInt32()]) == item.GetProperty("sha256").GetString());
        _baselineMatches = MatchesBaseline(_manifest);
    }

    [Fact] void should_cover_each_vector_exactly_once() => _indexes.ShouldEqual(Enumerable.Range(0, _vectors.Length).ToArray());
    [Fact] void should_have_unique_ids() => _ids.Distinct().Count().ShouldEqual(_ids.Length);
    [Fact] void should_use_stable_kebab_case_ids() => _ids.All(CaseId.IsMatch).ShouldBeTrue();
    [Fact] void should_record_the_case_count() => _manifest.GetProperty("caseCount").GetInt32().ShouldEqual(_vectors.Length);
    [Fact] void should_include_cases() => _cases.ShouldNotBeEmpty();
    [Fact] void should_reference_the_vector_file() => _cases.All(item => item.GetProperty("file").GetString() == VectorFile).ShouldBeTrue();
    [Fact] void should_match_case_content() => _caseHashesMatch.ShouldBeTrue();
    [Fact] void should_bind_ids_and_file_hashes_to_the_versioned_baseline() => _baselineMatches.ShouldBeTrue();
}

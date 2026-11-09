// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Identity.for_VectorArtifacts;

public partial class when_checking_the_arc_mcp_authorization_manifest : given.an_artifact
{
    string[] _listedFiles;
    string[] _actualFiles;
    bool _hashesMatch;

    [GeneratedRegex(@"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)$", RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex SemanticVersion { get; }

    void Because()
    {
        var files = _manifest.GetProperty("files").EnumerateArray().ToArray();
        _listedFiles = files.Select(file => file.GetProperty("path").GetString() ?? string.Empty).Order().ToArray();
        _actualFiles = Directory.GetFiles(_directory, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(_directory, path).Replace('\\', '/'))
            .Except(["manifest.json", "CHANGELOG.md"]).Order().ToArray();
        _hashesMatch = files.All(file => Hash(File.ReadAllBytes(Path.Combine(_directory, file.GetProperty("path").GetString()))) == file.GetProperty("sha256").GetString());
    }

    [Fact] void should_name_the_spec() => _manifest.GetProperty("spec").GetString().ShouldEqual("arc-mcp-authorization");
    [Fact] void should_name_the_contract_version() => _manifest.GetProperty("contractVersion").GetInt32().ShouldEqual(1);
    [Fact] void should_name_the_schema() => _manifest.GetProperty("schema").GetInt32().ShouldEqual(1);
    [Fact] void should_use_semantic_versioning() => SemanticVersion.IsMatch(_version).ShouldBeTrue();
    [Fact] void should_list_every_vector_file_once() => _listedFiles.ShouldEqual(_actualFiles);
    [Fact] void should_include_the_locked_vector_file() => _listedFiles.ShouldContain(VectorFile);
    [Fact] void should_match_the_stored_file_bytes() => _hashesMatch.ShouldBeTrue();
}

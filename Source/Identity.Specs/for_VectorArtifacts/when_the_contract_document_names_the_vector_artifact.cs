// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_VectorArtifacts;

public class when_the_contract_document_names_the_vector_artifact : given.an_artifact
{
    string _contract;
    string[] _changelog;

    void Because()
    {
        _contract = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "arc-mcp-authorization-v1.md"));
        _changelog = File.ReadAllLines(Path.Combine(_directory, "CHANGELOG.md"));
    }

    [Fact] void should_record_the_artifact_version() => _contract.ShouldContain($"Vector artifact version: {_version}");
    [Fact] void should_record_the_contract_id() => _contract.ShouldContain($"Spec id: {_manifest.GetProperty("spec").GetString()}");
    [Fact] void should_record_the_contract_version() => _contract.ShouldContain($"Version: {_manifest.GetProperty("contractVersion").GetInt32()}");
    [Fact] void should_record_the_revision() => _contract.ShouldContain(_manifest.GetProperty("revision").GetProperty("mcpAuthorization").GetString());
    [Fact] void should_have_a_matching_changelog_entry() => _changelog.ShouldContain($"## {_version}");
    [Fact] void should_put_the_current_version_first() => _changelog.First(line => line.StartsWith("## ", StringComparison.Ordinal)).ShouldEqual($"## {_version}");
}

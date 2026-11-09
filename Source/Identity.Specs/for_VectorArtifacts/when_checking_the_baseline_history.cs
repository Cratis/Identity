// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_VectorArtifacts;

public class when_checking_the_baseline_history : given.an_artifact
{
    string[] _baselines;
    string[] _releases;

    void Because()
    {
        _baselines = Directory.GetFiles(Path.Combine(_directory, "baselines"))
            .Select(path => Path.GetFileName(path)).Order(StringComparer.Ordinal).ToArray();
        _releases = File.ReadAllLines(Path.Combine(_directory, "CHANGELOG.md"))
            .Where(line => line.StartsWith("## ", StringComparison.Ordinal))
            .Select(line => $"v{line[3..]}.json").Order(StringComparer.Ordinal).ToArray();
    }

    [Fact] void should_retain_a_baseline_for_every_changelog_version() => _baselines.ShouldEqual(_releases);
    [Fact] void should_include_the_current_version() => _baselines.ShouldContain($"v{_version}.json");
    [Fact] void should_include_the_initial_release() => _baselines.ShouldContain("v1.0.0.json");
    [Fact] void should_not_include_future_versions() => _baselines.All(name => Version.Parse(name[1..^5]) <= Version.Parse(_version)).ShouldBeTrue();
}

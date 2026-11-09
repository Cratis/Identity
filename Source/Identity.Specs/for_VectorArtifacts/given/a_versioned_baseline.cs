// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Identity.for_VectorArtifacts.given;

public class a_versioned_baseline : an_artifact
{
    JsonElement _baseline;

    protected bool MatchesBaseline(JsonElement manifest) =>
        manifest.GetProperty("version").GetString() == _baseline.GetProperty("version").GetString() &&
        MatchesBindings(manifest.GetProperty("cases"), _baseline.GetProperty("cases"), "id") &&
        MatchesBindings(manifest.GetProperty("files"), _baseline.GetProperty("files"), "path");

    static bool MatchesBindings(JsonElement entries, JsonElement bindings, string key)
    {
        var actual = entries.EnumerateArray().ToDictionary(item => item.GetProperty(key).GetString()!, item => item.GetProperty("sha256").GetString());
        var expected = bindings.EnumerateObject().ToDictionary(item => item.Name, item => item.Value.GetString());
        return actual.Count == expected.Count && actual.All(pair => expected.TryGetValue(pair.Key, out var hash) && pair.Value == hash);
    }

    void Establish()
    {
        using var baseline = JsonDocument.Parse(File.ReadAllText(Path.Combine(_directory, "baselines", $"v{_version}.json")));
        _baseline = baseline.RootElement.Clone();
    }
}

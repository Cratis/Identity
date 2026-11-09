// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Contracts.Check;

/// <summary>Runs the explicitly requested released-ref gate without an optional skip path.</summary>
internal static class ReleasedSchemaCheck
{
    /// <summary>Checks the working tree against a Git ref and fails closed on missing history or Git.</summary>
    /// <param name="reference">The trusted base or released ref.</param>
    /// <param name="solution">The solution path, used to locate the repository.</param>
    /// <param name="output">The diagnostic writer.</param>
    /// <returns>Zero only when released history is intact.</returns>
    internal static async Task<int> Run(string reference, string solution, TextWriter output)
    {
        try
        {
            var reader = new ReleasedSchemaReader(Path.GetDirectoryName(Path.GetFullPath(solution))!);
            var released = await reader.ReadReleased(reference);
            var violations = ReleasedSchemaCompatibility.Check(released, reader.ReadCurrent());
            foreach (var violation in violations) await output.WriteLineAsync($"event-schema {violation.Path} {violation.ReasonId}");
            await output.WriteLineAsync($"event-schema checked {released.Catalogue.Count} released generations; {violations.Count} violations");
            return violations.Count == 0 ? 0 : 1;
        }
        catch (Exception)
        {
            await output.WriteLineAsync("event-schema $ released_ref_unavailable");
            return 1;
        }
    }
}

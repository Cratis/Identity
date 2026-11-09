// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Xml.Linq;

namespace Cratis.Identity.Contracts.Check;

/// <summary>Ensures the explicit build inputs cover every production project in the solution.</summary>
internal static class ProductionAssemblyGuard
{
    /// <summary>Reports production outputs omitted from discovery.</summary>
    /// <param name="solution">The solution whose production projects must be scanned.</param>
    /// <param name="assemblies">The assemblies supplied by the checker's project references.</param>
    /// <returns>Diagnostics for missing outputs, or an empty production project set.</returns>
    internal static IReadOnlyList<string> Check(string solution, IEnumerable<Assembly> assemblies)
    {
        var root = Path.GetDirectoryName(Path.GetFullPath(solution))!;

        // Only the checker and specification projects are tools rather than production outputs.
        // New production projects fail closed until they are referenced by the checker.
        var projects = XDocument.Load(solution).Descendants("Project")
            .Select(project => (string)project.Attribute("Path")!)
            .Where(path => !path.EndsWith(".Specs.csproj", StringComparison.Ordinal) &&
                Path.GetFileName(path) != "Identity.Contracts.Check.csproj").ToArray();
        if (projects.Length == 0) return ["event-content $ production_projects_empty"];
        var scanned = assemblies.Select(assembly => assembly.GetName().Name).ToHashSet(StringComparer.Ordinal);
        return projects.Select(path =>
            XDocument.Load(Path.Combine(root, path)).Descendants("AssemblyName").SingleOrDefault()?.Value ?? Path.GetFileNameWithoutExtension(path))
            .Where(name => !scanned.Contains(name)).Order(StringComparer.Ordinal)
            .Select(name => $"event-content {name} production_assembly_not_scanned").ToArray();
    }
}

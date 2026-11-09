// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Events;
using Cratis.Identity.Contracts.Compliance;

namespace Cratis.Identity.Contracts.Check;

/// <summary>Runs the build-time contract checks and emits only schema diagnostics.</summary>
internal static class CheckRunner
{
    /// <summary>Checks coverage before discovering event registrations.</summary>
    /// <param name="assemblies">The production outputs supplied by MSBuild.</param>
    /// <param name="solution">The Identity solution path.</param>
    /// <param name="output">The diagnostic writer.</param>
    /// <returns>Zero for complete, valid schemas; one for violations.</returns>
    internal static int Run(IEnumerable<Assembly> assemblies, string solution, TextWriter output)
    {
        var inputs = assemblies.ToArray();
        var coverage = ProductionAssemblyGuard.Check(solution, inputs);
        foreach (var diagnostic in coverage) output.WriteLine(diagnostic);
        return coverage.Count == 0 ? Run(ReflectionShapeBuilder.Discover(inputs), output) : 1;
    }

    /// <summary>Returns a process-compatible status for the discovered event types.</summary>
    /// <param name="types">The discovered artifacts.</param>
    /// <param name="output">The diagnostic writer.</param>
    /// <returns>Zero for valid schemas; one for violations.</returns>
    internal static int Run(IEnumerable<Type> types, TextWriter output)
    {
        var discovered = types.ToArray();
        if (discovered.Length == 0)
        {
            output.WriteLine("event-content $ discovery_empty");
            return 1;
        }
        var shapes = discovered.Select(type => ReflectionShapeBuilder.Build(type, CatalogueChecker.HasAccountSubject(CurrentType(type)))).ToArray();
        var count = 0;
        for (var index = 0; index < shapes.Length; index++)
        {
            var shape = shapes[index];
            foreach (var violation in CheckEvent(discovered[index], shape))
            {
                output.WriteLine($"event-content {discovered[index].FullName} {shape.TypeId}/{shape.Generation} {shape.Visibility} {violation.Path} {violation.ReasonId}");
                count++;
            }
        }
        foreach (var violation in CatalogueChecker.CheckIdentities(shapes))
        {
            output.WriteLine($"event-content $ {violation.ReasonId}");
            count++;
        }
        foreach (var row in CatalogueChecker.Rows.Where(row => row.TypeId is not null && !discovered.Any(type => Matches(type, row))))
        {
            output.WriteLine($"event-content {row.Name} {row.TypeId}/1 {row.Visibility} $ allocated_event_missing");
            count++;
        }
        count += CheckSchemas(discovered, shapes, output);
        output.WriteLine($"event-content checked {shapes.Length} event shapes; {count} violations");
        return count == 0 ? 0 : 1;
    }

    static IEnumerable<Violation> CheckEvent(Type type, EventShape shape)
    {
        var current = CurrentType(type);
        var row = CatalogueChecker.Rows.SingleOrDefault(candidate => Matches(current, candidate));

        // Prior generations retain their own reviewed fields, not the current catalogue's fields.
        var catalogueViolations = type == current ? CatalogueChecker.Check(shape, row) : [];
        return ContentRules.Check(shape, SafeConceptRegistry.Entries).Concat(catalogueViolations);
    }

    static int CheckSchemas(Type[] discovered, EventShape[] shapes, TextWriter output)
    {
        var schemas = discovered.Select((type, index) => SchemaCompatibility.Describe(type, shapes[index]));
        var violations = SchemaCompatibility.Check(schemas, SchemaCompatibility.Baseline, SchemaCompatibility.FixtureExists);
        foreach (var violation in violations) output.WriteLine($"event-schema {violation.Path} {violation.ReasonId}");
        return violations.Count;
    }

    static Type CurrentType(Type type) => type.GetCustomAttribute<EventTypeGenerationForAttribute>()?.EventTypeClrType ?? type;

    static bool Matches(Type type, CatalogueRow row) => type.Assembly.GetName().Name == row.Assembly &&
        type.FullName == $"{row.Assembly}.{row.Area}.Events.{row.Name}";
}

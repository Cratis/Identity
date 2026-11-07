// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Compliance;

namespace Cratis.Identity.Contracts.Check;

/// <summary>Runs the build-time contract checks and emits only schema diagnostics.</summary>
internal static class CheckRunner
{
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
        var shapes = discovered.Select(type => ReflectionShapeBuilder.Build(type, CatalogueChecker.HasAccountSubject(type))).ToArray();
        var count = 0;
        for (var index = 0; index < shapes.Length; index++)
        {
            var shape = shapes[index];
            var row = CatalogueChecker.Rows.SingleOrDefault(candidate => discovered[index].FullName == $"Cratis.Identity.Contracts.{candidate.Area}.Events.{candidate.Name}");
            foreach (var violation in ContentRules.Check(shape, SafeConceptRegistry.Entries).Concat(CatalogueChecker.Check(shape, row)))
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
        foreach (var row in CatalogueChecker.Rows.Where(row => row.TypeId is not null && !shapes.Any(shape => shape.Name == row.Name)))
        {
            output.WriteLine($"event-content {row.Name} {row.TypeId}/1 {row.Visibility} $ allocated_event_missing");
            count++;
        }
        output.WriteLine($"event-content checked {shapes.Length} event shapes; {count} violations");
        return count == 0 ? 0 : 1;
    }
}

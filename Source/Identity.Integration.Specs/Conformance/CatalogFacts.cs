// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Identity.Integration.Conformance;

/// <summary>
/// Discovers declared instance facts, including private facts and attributes derived from FactAttribute.
/// </summary>
public static class CatalogFacts
{
    /// <summary>
    /// Gets the facts declared by a spec. Base classes are contexts, not owners of catalogue proofs.
    /// </summary>
    /// <param name="type">The declaring spec type.</param>
    /// <returns>The declared facts and theories.</returns>
    public static IEnumerable<MethodInfo> DeclaredBy(Type type) =>
        type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            .Where(_ => _.IsDefined(typeof(FactAttribute), inherit: true));

    /// <summary>
    /// Discovers each requirement-to-fact mapping on the supplied spec types.
    /// </summary>
    /// <param name="types">The spec types.</param>
    /// <returns>The requirement and its discovered fact.</returns>
    public static IReadOnlyList<(string Requirement, MethodInfo Fact)> Tagged(IEnumerable<Type> types) =>
        types.SelectMany(type => type.GetCustomAttributes<RequirementAttribute>()
            .SelectMany(tag => DeclaredBy(type).Select(fact => (tag.Id, fact)))).ToArray();

    /// <summary>
    /// Checks that every requirement has a discovered fact, without accepting an empty discovery result.
    /// </summary>
    /// <param name="requirements">The catalogue identifiers.</param>
    /// <param name="facts">The discovered mappings.</param>
    /// <returns>Whether every requirement has a discovered fact.</returns>
    public static bool Covers(IEnumerable<string> requirements, IReadOnlyList<(string Requirement, MethodInfo Fact)> facts) =>
        facts.Count > 0 && requirements.All(requirement => facts.Any(_ => _.Requirement == requirement));

    /// <summary>
    /// Checks that every discovered tagged fact refers to a catalogue entry.
    /// </summary>
    /// <param name="requirements">The catalogue identifiers.</param>
    /// <param name="facts">The discovered mappings.</param>
    /// <returns>Whether all discovered fact mappings are known.</returns>
    public static bool OnlyKnown(IEnumerable<string> requirements, IReadOnlyList<(string Requirement, MethodInfo Fact)> facts) =>
        facts.All(fact => requirements.Contains(fact.Requirement, StringComparer.Ordinal));
}

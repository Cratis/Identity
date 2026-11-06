// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.ReadModels;
using Cratis.Identity.Accounts.Lookup;

namespace Cratis.Identity.for_ReadModelIndexes;

/// <summary>
/// Chronicle resolves composite index paths through CLR property names when the client registers read models,
/// and refuses to connect when a path segment is unknown. This guards every declaration in the Identity assembly.
/// </summary>
public class when_resolving_declared_index_paths
{
    [Fact]
    void should_resolve_every_composite_index_path_through_clr_properties()
    {
        var unresolved = typeof(UpstreamKeyLookup).Assembly.GetTypes()
            .SelectMany(type => type.GetCustomAttributes<CompositeIndexAttribute>().SelectMany(declaration => declaration.Properties.Select(path => (type, path))))
            .Where(declaration => !Resolves(declaration.type, declaration.path))
            .Select(declaration => $"{declaration.type.Name}: {declaration.path}")
            .ToArray();

        unresolved.ShouldBeEmpty();
    }

    [Fact]
    void should_index_the_upstream_key_hash_of_each_reservation() =>
        typeof(UpstreamKeyReservation).GetProperty(nameof(UpstreamKeyReservation.KeyHash))!.IsDefined(typeof(IndexAttribute)).ShouldBeTrue();

    static bool Resolves(Type type, string path)
    {
        foreach (var segment in path.Split('.'))
        {
            var property = type.GetProperty(segment.EndsWith("[]", StringComparison.Ordinal) ? segment[..^2] : segment, BindingFlags.Public | BindingFlags.Instance);
            if (property is null)
            {
                return false;
            }

            var propertyType = property.PropertyType;
            type = propertyType != typeof(string) && propertyType.IsGenericType && typeof(System.Collections.IEnumerable).IsAssignableFrom(propertyType)
                ? propertyType.GetGenericArguments()[^1]
                : Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        }

        return true;
    }
}

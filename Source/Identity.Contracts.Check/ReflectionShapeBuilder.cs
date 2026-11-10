// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Events;
using Cratis.Concepts;

namespace Cratis.Identity.Contracts.Check;

/// <summary>Reads declared schemas using Chronicle's compliance metadata provider.</summary>
internal static class ReflectionShapeBuilder
{
    /// <summary>Builds the shape for a discovered generation.</summary>
    /// <param name="type">The event CLR type.</param>
    /// <param name="complianceSubject">Whether the catalogue resolves an account subject.</param>
    /// <returns>The event schema.</returns>
    internal static EventShape Build(Type type, bool complianceSubject = false)
    {
        var current = type.GetCustomAttribute<EventTypeAttribute>();
        var previous = type.GetCustomAttribute<EventTypeGenerationForAttribute>();
        var generation = current?.Generation.Value ?? previous?.Generation.Value ?? 0;
        return new(
            type.Name,
            ResolveTypeId(type),
            generation,
            current is not null ? "EventType" : previous is not null ? "EventTypeGenerationFor" : "Unmarked",
            type.IsDefined(typeof(PublicAttribute)) || type.GetEventStoreName() is not null ? "Public" : "Internal",
            complianceSubject,
            Members(type, new HashSet<Type>()).ToArray(),
            ExplicitTypeId(type).Length > 0);
    }

    /// <summary>Resolves the event type id the way Chronicle does: the attribute id when given, otherwise the short CLR type name.</summary>
    /// <param name="type">The event artifact; a prior generation resolves the id of the type it refers to.</param>
    /// <returns>The resolved event type id, or empty for a type that is not an event artifact.</returns>
    internal static string ResolveTypeId(Type type)
    {
        var explicitId = ExplicitTypeId(type);
        if (explicitId.Length > 0) return explicitId;
        if (type.IsDefined(typeof(EventTypeAttribute), false)) return type.Name;
        return type.GetCustomAttribute<EventTypeGenerationForAttribute>()?.EventTypeClrType.Name ?? string.Empty;
    }

    /// <summary>Discovers current, prior, and explicitly registered event artifacts.</summary>
    /// <param name="assembly">An Identity assembly.</param>
    /// <param name="registered">Additional explicit event artifacts.</param>
    /// <returns>The discovered types.</returns>
    internal static IReadOnlyList<Type> Discover(Assembly assembly, IEnumerable<Type>? registered = null) =>
        assembly.GetTypes().Where(type => type.IsDefined(typeof(EventTypeAttribute)) || type.IsDefined(typeof(EventTypeGenerationForAttribute)))
            .Concat(registered ?? []).Distinct().OrderBy(type => type.FullName, StringComparer.Ordinal).ToArray();

    /// <summary>Discovers event registrations across all production outputs.</summary>
    /// <param name="assemblies">Every production Identity assembly.</param>
    /// <returns>The discovered types, including non-exported registrations.</returns>
    internal static IReadOnlyList<Type> Discover(IEnumerable<Assembly> assemblies) => assemblies.SelectMany(assembly => Discover(assembly))
        .Distinct().OrderBy(type => type.FullName, StringComparer.Ordinal).ToArray();

    /// <summary>Identifies a serialized primitive for registry comparison.</summary>
    /// <param name="type">The underlying type.</param>
    /// <returns>The registry type identity.</returns>
    internal static string PrimitiveName(Type type) => type == typeof(string) ? "string" : type == typeof(Guid) ? "Guid" :
        type == typeof(int) || type == typeof(long) || type == typeof(uint) || type == typeof(ulong) ? "integer" : type.FullName ?? type.Name;

    static string ExplicitTypeId(Type type) => type.GetCustomAttribute<EventTypeAttribute>()?.Id.Value ??
        type.GetCustomAttribute<EventTypeGenerationForAttribute>()?.EventTypeClrType.GetCustomAttribute<EventTypeAttribute>()?.Id.Value ?? string.Empty;

    static MemberShape[] Members(Type type, HashSet<Type> ancestors)
    {
        if (!ancestors.Add(type)) return [];
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(property => property.GetIndexParameters().Length == 0)
            .Select(property => Member(
                property.Name,
                property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? property.Name,
                property.PropertyType,
                new PIIMetadataProvider().CanProvide(property),
                new NullabilityInfoContext().Create(property).ReadState == NullabilityState.Nullable,
                ancestors));
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance).Select(field => Member(
            field.Name,
            field.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? field.Name,
            field.FieldType,
            field.IsDefined(typeof(PIIAttribute)) || field.FieldType.IsDefined(typeof(PIIAttribute)) || type.IsDefined(typeof(PIIAttribute)),
            new NullabilityInfoContext().Create(field).ReadState == NullabilityState.Nullable,
            ancestors));
        var result = properties.Concat(fields).ToArray();
        ancestors.Remove(type);
        return result;
    }

    static MemberShape Member(string name, string wireName, Type type, bool pii, bool nullable, HashSet<Type> ancestors)
    {
        var actual = Nullable.GetUnderlyingType(type) ?? type;
        Type? concept = null;
        for (var candidate = actual; candidate is not null; candidate = candidate.BaseType)
        {
            if (candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(ConceptAs<>))
            {
                concept = actual;
                actual = candidate.GetGenericArguments()[0];
                break;
            }
        }
        var forbidden = Forbidden(actual);
        var open = Open(actual);
        var element = actual.IsArray ? actual.GetElementType() : actual.GetInterfaces().Append(actual)
            .FirstOrDefault(candidate => candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>))?.GetGenericArguments()[0];
        IReadOnlyList<MemberShape> children = [];
        if (!forbidden && !open && !Scalar(actual))
        {
            children = element is not null
                ? [Member("[]", "[]", element, pii, false, ancestors)]
                : Members(actual, ancestors).ToArray();
        }
        if (concept is not null)
        {
            children = children.Concat(Members(concept, ancestors).Where(member => member.ClrName != "Value")).ToArray();
        }
        return new(
            name,
            wireName,
            type.FullName ?? type.Name,
            pii || actual.IsDefined(typeof(PIIAttribute)),
            forbidden,
            open,
            nullable || Nullable.GetUnderlyingType(type) is not null,
            concept?.FullName,
            concept is null ? null : PrimitiveName(actual),
            children);
    }

    static bool Scalar(Type type) => type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(Guid) || type == typeof(decimal) ||
        type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(DateOnly) || type == typeof(TimeOnly) || type == typeof(TimeSpan);

    static bool Open(Type type) => type == typeof(object) || type == typeof(JsonElement) || type == typeof(JsonDocument) ||
        type.Namespace?.StartsWith("System.Text.Json.Nodes", StringComparison.Ordinal) == true ||
        HasBaseType(type, "Newtonsoft.Json.Linq.JToken") || type.IsDefined(typeof(JsonPolymorphicAttribute)) || type.IsDefined(typeof(JsonDerivedTypeAttribute)) ||
        ((type.IsInterface || type.IsAbstract) && !typeof(IEnumerable).IsAssignableFrom(type)) || typeof(IDictionary).IsAssignableFrom(type) ||
        type.GetInterfaces().Append(type).Any(candidate => candidate.IsGenericType &&
            (candidate.GetGenericTypeDefinition() == typeof(IDictionary<,>) || candidate.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>))) ||
        (typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string) && !type.IsArray &&
            !type.GetInterfaces().Append(type).Any(candidate => candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>)));

    static bool Forbidden(Type type) => type == typeof(byte[]) || type == typeof(Memory<byte>) || type == typeof(ReadOnlyMemory<byte>) ||
        type == typeof(ArraySegment<byte>) || type == typeof(Span<byte>) || type == typeof(ReadOnlySpan<byte>) || type == typeof(System.Security.SecureString) ||
        typeof(System.Security.Claims.ClaimsPrincipal).IsAssignableFrom(type) || typeof(System.Security.Claims.ClaimsIdentity).IsAssignableFrom(type) ||
        typeof(System.Net.NetworkCredential).IsAssignableFrom(type) || typeof(Stream).IsAssignableFrom(type) || typeof(AsymmetricAlgorithm).IsAssignableFrom(type) ||
        typeof(SymmetricAlgorithm).IsAssignableFrom(type) || type == typeof(RSAParameters) || type == typeof(ECParameters) ||
        type.Namespace?.StartsWith("System.Security.Cryptography.X509Certificates", StringComparison.Ordinal) == true ||
        type.Namespace?.StartsWith("Microsoft.IdentityModel.Tokens", StringComparison.Ordinal) == true ||
        HasBaseType(type, "System.IdentityModel.Tokens.Jwt.JwtSecurityToken") || HasBaseType(type, "Microsoft.IdentityModel.JsonWebTokens.JsonWebToken");

    static bool HasBaseType(Type type, string fullName)
    {
        for (var candidate = type; candidate is not null; candidate = candidate.BaseType)
        {
            if (candidate.FullName == fullName) return true;
        }
        return false;
    }
}

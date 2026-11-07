// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Identity.Accounts;
using Cratis.Identity.Hosting;

namespace Cratis.Identity.Architecture;

public class when_inspecting_mongodb_usage : Specification
{
    static readonly Type[] _legacyAllowList = [];
    Type[] _inspected;
    string[] _violations;

    void Because()
    {
        _inspected = typeof(Account).Assembly.GetTypes().Where(type => !Allowed(type)).ToArray();
        _violations = _inspected.SelectMany(type => Signatures(type).Where(UsesMongo).Select(signature => $"{type.FullName}: {signature}")).ToArray();
    }

    [Fact] void should_inspect_non_infrastructure_types() => _inspected.ShouldNotBeEmpty();
    [Fact] void should_keep_mongodb_out_of_domain_signatures() => _violations.ShouldBeEmpty();

    static bool Allowed(Type type) => _legacyAllowList.Contains(type) ||
        type.Namespace == "Cratis.Identity.ConsentFlow.Operational" ||
        type.Namespace == "Cratis.Identity.Revocation.Operational" ||
        type == typeof(Authorization.FencedTokenStore) || type.DeclaringType == typeof(Authorization.FencedTokenStore) ||
        new[] { "Cratis.Identity.Operational", "Cratis.Identity.Storage", "Cratis.Identity.Keys" }.Any(prefix => type.Namespace?.StartsWith(prefix, StringComparison.Ordinal) == true) ||
        type == typeof(IdentityServiceCollectionExtensions) || type == typeof(MongoClientFactory) || type.DeclaringType == typeof(IdentityServiceCollectionExtensions);

    static IEnumerable<Type> Signatures(Type type)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        return type.GetFields(flags).Select(field => field.FieldType)
            .Concat(type.GetProperties(flags).Select(property => property.PropertyType))
            .Concat(type.GetConstructors(flags).SelectMany(constructor => constructor.GetParameters().Select(parameter => parameter.ParameterType)))
            .Concat(type.GetMethods(flags).SelectMany(method => method.GetParameters().Select(parameter => parameter.ParameterType).Append(method.ReturnType)));
    }

    static bool UsesMongo(Type type) => type.Namespace?.StartsWith("MongoDB.", StringComparison.Ordinal) == true ||
        (type.HasElementType && UsesMongo(type.GetElementType())) ||
        (type.IsGenericType && type.GetGenericArguments().Any(UsesMongo));
}

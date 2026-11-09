// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Identity.Revocation;

namespace Cratis.Identity.RevocationSpecs;

public class when_inspecting_retirement_store_dependencies : Specification
{
    Type[] _types;
    Type[] _restoredDependencies;

    void Because()
    {
        _types = typeof(RecoveryGate).Assembly.GetTypes().Where(type =>
            type.Namespace == "Cratis.Identity.Keys" || type.Namespace == "Cratis.Identity.Clients" ||
            type == typeof(Protocol.Stores.ConfigurationApplicationStore) || type == typeof(Protocol.Stores.ConfigurationProtocolCatalog)).ToArray();
        _restoredDependencies = _types.SelectMany(type =>
            type.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Select(field => field.FieldType)
                .Concat(type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .SelectMany(constructor => constructor.GetParameters().Select(parameter => parameter.ParameterType))))
            .Where(type => type.ToString().Contains("MongoDB", StringComparison.Ordinal) || type.ToString().Contains("Cratis.Chronicle", StringComparison.Ordinal)).ToArray();
    }

    [Fact] void should_inspect_key_types() => _types.Any(type => type.Namespace == "Cratis.Identity.Keys").ShouldBeTrue();
    [Fact] void should_inspect_client_configuration() => _types.Contains(typeof(Protocol.Stores.ConfigurationApplicationStore)).ShouldBeTrue();
    [Fact] void should_keep_retirement_sources_outside_the_restore_set() => _restoredDependencies.ShouldBeEmpty();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Clients;

namespace Cratis.Identity.Replay.for_EventLog.given;

internal static class EventSeeder
{
    static readonly Dictionary<Type, object> _values = new()
    {
        [typeof(UpstreamIssuer)] = new UpstreamIssuer("https://issuer.example.test"),
        [typeof(AccountSubject)] = new AccountSubject("00000000-0000-4000-8000-000000000001"),
        [typeof(SecretDigest)] = new SecretDigest("v1.AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"),
        [typeof(string)] = "seed-value",
        [typeof(Guid)] = Guid.Parse("00000000-0000-0000-0000-000000000001"),
        [typeof(DateTimeOffset)] = DateTimeOffset.UnixEpoch,
        [typeof(bool)] = true,
        [typeof(int)] = 1,
        [typeof(long)] = 1L,
        [typeof(ulong)] = 1UL
    };

    internal static object Seed(Type type)
    {
        if (!EventRegistry.Types.Contains(type))
        {
            throw new InvalidOperationException($"Event seeder has no registered event: {type.FullName}");
        }

        var constructor = type.GetConstructors().Single();
        return constructor.Invoke(constructor.GetParameters().Select(parameter => Value(parameter.ParameterType)).ToArray());
    }

    internal static object Value(Type type)
    {
        if (_values.TryGetValue(type, out var value)) return value;
        if (type.IsEnum) return Enum.GetValues(type).GetValue(0)!;
        if (type.IsArray) return Array.CreateInstance(type.GetElementType()!, 0);
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)) return Array.CreateInstance(type.GenericTypeArguments[0], 0);
        if (Nullable.GetUnderlyingType(type) is { } underlying) return Value(underlying);

        for (var parent = type.BaseType; parent is not null; parent = parent.BaseType)
        {
            if (parent.IsGenericType && parent.GetGenericTypeDefinition() == typeof(ConceptAs<>))
            {
                var valueType = parent.GenericTypeArguments[0];
                return Activator.CreateInstance(type, Value(valueType))!;
            }
        }

        throw new InvalidOperationException($"Unsupported event field kind: {type.FullName}");
    }
}

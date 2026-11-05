// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Events;
using Cratis.Identity.Contracts;

namespace Cratis.Identity.Clients.for_ClientContracts.given;

public static class ClientPayloadScanner
{
    public static IEnumerable<string> Violations(Type payload)
    {
        if (payload.IsDefined(typeof(PublicAttribute)))
        {
            yield return "public";
        }

        foreach (var property in payload.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.PropertyType == typeof(string) || property.Name == "FactId" || IsForbidden(property))
            {
                yield return property.Name;
            }
        }
    }

    static bool IsForbidden(PropertyInfo property)
    {
        var sensitive = new[] { "Secret", "Token", "Code", "Password" }.Any(term => property.Name.Contains(term, StringComparison.OrdinalIgnoreCase));
        return sensitive && !((property.Name == "ClientSecretHash" && property.PropertyType == typeof(SecretDigest)) ||
            (property.Name == "ClientSecretId" && property.PropertyType == typeof(OpaqueReferenceId)));
    }
}

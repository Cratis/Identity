// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Events;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Check;
using Cratis.Identity.Contracts.Compliance;

namespace Cratis.Identity.Accounts.given;

public static class AccountPayloadScanner
{
    public static IEnumerable<string> Violations(Type type) => ContentRules.Check(
        ReflectionShapeBuilder.Build(type, CatalogueChecker.HasAccountSubject(type)),
        SafeConceptRegistry.Entries).Select(violation => $"{type.Name}.{violation.Path}")
        .Concat(LegacyViolations(type).Select(property => $"{type.Name}.{property.Name}")).Distinct();

    static IEnumerable<PropertyInfo> LegacyViolations(Type type) => type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
        .Where(property => property.PropertyType == typeof(string) ||
            (type.IsDefined(typeof(PublicAttribute)) && property.PropertyType == typeof(UpstreamSubject)) ||
            (property.PropertyType.IsDefined(typeof(PIIAttribute)) && !property.PropertyType.IsDefined(typeof(ComplianceDetailsAttribute))));
}

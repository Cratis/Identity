// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Check;
using Cratis.Identity.Contracts.Compliance;

namespace Cratis.Identity.Accounts.given;

public static class AccountPayloadScanner
{
    public static IEnumerable<string> Violations(Type type) => ContentRules.Check(
        ReflectionShapeBuilder.Build(type, CatalogueChecker.HasAccountSubject(type)),
        SafeConceptRegistry.Entries).Select(violation => $"{type.Name}.{violation.Path}").Distinct();
}

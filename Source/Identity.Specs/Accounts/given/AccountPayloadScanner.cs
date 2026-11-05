// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Events;
using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.given;

public static class AccountPayloadScanner
{
    public static IEnumerable<string> Violations(Type payload)
    {
        foreach (var property in payload.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.PropertyType == typeof(string))
            {
                yield return property.Name;
            }

            if (payload.IsDefined(typeof(PublicAttribute)) && property.PropertyType == typeof(UpstreamSubject))
            {
                yield return property.Name;
            }

            if (property.PropertyType.IsDefined(typeof(PIIAttribute)) && !property.PropertyType.IsDefined(typeof(ComplianceDetailsAttribute)))
            {
                yield return property.Name;
            }
        }
    }
}

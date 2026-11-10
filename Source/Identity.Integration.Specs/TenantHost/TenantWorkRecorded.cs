// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;
using Cratis.Chronicle.Events;
using Cratis.Identity.Contracts.Accounts;

namespace Specs.TenantHost.Work;

/// <summary>Records the subject and request tenant observed by the test-only host command.</summary>
/// <param name="Subject">The authenticated session subject.</param>
/// <param name="Tenant">The Arc-resolved request tenant.</param>
[EventType]
public record TenantWorkRecorded(AccountSubject Subject, TenantId Tenant);

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Tenancy;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Identity.Contracts.Accounts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Specs.TenantHost.Work;

/// <summary>A host-owned command outside the Identity namespace and realm routes.</summary>
/// <param name="WorkId">The host event source.</param>
[Command, Authorize]
public record RecordTenantWork(EventSourceId WorkId)
{
    public EventForEventSourceId Handle(IHttpContextAccessor http, ITenantIdAccessor tenant) =>
        new(WorkId, new TenantWorkRecorded(new AccountSubject(http.HttpContext.User.FindFirstValue("sub")), tenant.Current));
}

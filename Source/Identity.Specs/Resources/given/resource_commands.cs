// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Identity.Accounts.given;
using Cratis.Identity.Configuration;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Resources;
using Cratis.Identity.Contracts.Resources.Events;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.Resources.given;

public class resource_commands : Specification
{
    protected static readonly DateTimeOffset Now = DateTimeOffset.UnixEpoch.AddDays(10);
    protected ResourceId _id;
    protected AccountClock _clock;
    private protected ResourceRegistration _registered;
    private protected IResourceCatalog _catalog;
    private protected ResourceRegistrationReducer _reducer;
    private protected EventContext _context;

    void Establish()
    {
        _id = ResourceId.New();
        _clock = new() { Now = Now };
        _catalog = new ResourceCatalog(Options.Create(new IdentityOptions()));
        _reducer = new();
        _context = EventContext.EmptyWithEventSourceId(_id.Value);
        _registered = _reducer.On(Registered(), null, _context);
    }

    private protected static ResourceRegistrationRecorded Registered(string indicator = "https://api.example", string audience = "api", string prefix = "api") =>
        new(indicator, audience, prefix, new("API"), FactId.New());

    private protected ResourceRegistration WithScope() => _reducer.On(new ResourceScopeRegistrationRecorded("api:read", new("Read API"), FactId.New()), _registered, _context);
}

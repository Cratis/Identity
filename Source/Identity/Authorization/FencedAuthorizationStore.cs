// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Identity.Revocation;
using Microsoft.Extensions.Options;
using OpenIddict.MongoDb;
using OpenIddict.MongoDb.Models;

namespace Cratis.Identity.Authorization;

/// <summary>Preserves the creation epoch across all authorization property replacements.</summary>
/// <param name="context">The stock Mongo context.</param>
/// <param name="options">The stock options.</param>
/// <param name="gate">The recovery continuity guard.</param>
internal sealed class FencedAuthorizationStore(IOpenIddictMongoDbContext context, IOptionsMonitor<OpenIddictMongoDbOptions> options, RecoveryGate gate)
    : OpenIddictMongoDbAuthorizationStore<OpenIddictMongoDbAuthorization>(context, options)
{
    /// <inheritdoc/>
    public override async ValueTask CreateAsync(OpenIddictMongoDbAuthorization authorization, CancellationToken cancellationToken)
    {
        authorization.Properties = RecoveryEpochStamp.Create(authorization.Properties, gate, RestrictionClass.Authorizations);
        await base.CreateAsync(authorization, cancellationToken);
    }

    /// <inheritdoc/>
    public override async ValueTask SetPropertiesAsync(OpenIddictMongoDbAuthorization authorization, ImmutableDictionary<string, JsonElement> properties, CancellationToken cancellationToken)
    {
        var original = authorization.Properties;
        await base.SetPropertiesAsync(authorization, RecoveryEpochStamp.Preserve(original, properties), cancellationToken);
        authorization.Properties = RecoveryEpochStamp.Reapply(original, authorization.Properties);
    }

    /// <inheritdoc/>
    public override async ValueTask UpdateAsync(OpenIddictMongoDbAuthorization authorization, CancellationToken cancellationToken)
    {
        var existing = await FindByIdAsync(authorization.Id.ToString(), cancellationToken) ?? throw new RevocationPending();
        RecoveryEpochStamp.DemandUnchanged(existing.Properties, authorization.Properties);
        await base.UpdateAsync(authorization, cancellationToken);
    }
}

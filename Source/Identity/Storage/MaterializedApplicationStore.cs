// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;
using OpenIddict.MongoDb;
using OpenIddict.MongoDb.Models;

namespace Cratis.Identity.Storage;

/// <summary>Inherits stock ObjectId-compatible reads but refuses a second client configuration writer.</summary>
/// <typeparam name="TApplication">The Mongo application type.</typeparam>
/// <param name="context">The stock Mongo context.</param>
/// <param name="options">The stock Mongo options.</param>
public class MaterializedApplicationStore<TApplication>(IOpenIddictMongoDbContext context, IOptionsMonitor<OpenIddictMongoDbOptions> options)
    : OpenIddictMongoDbApplicationStore<TApplication>(context, options)
    where TApplication : OpenIddictMongoDbApplication
{
    const string Message = "client configuration is written by Chronicle facts";

    /// <inheritdoc/>
    public override ValueTask CreateAsync(TApplication application, CancellationToken cancellationToken) => throw new InvalidOperationException(Message);

    /// <inheritdoc/>
    public override ValueTask UpdateAsync(TApplication application, CancellationToken cancellationToken) => throw new InvalidOperationException(Message);

    /// <inheritdoc/>
    public override ValueTask DeleteAsync(TApplication application, CancellationToken cancellationToken) => throw new InvalidOperationException(Message);
}

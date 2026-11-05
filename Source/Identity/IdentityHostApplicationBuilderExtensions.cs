// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Hosting;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extension methods for adding Cratis Identity to a host application builder.
/// </summary>
public static class IdentityHostApplicationBuilderExtensions
{
    /// <summary>
    /// Adds Cratis Identity: storage, upstream login, the OpenIddict authorization server and the endpoints. Options are
    /// bound from the <c language="text">Identity</c> configuration section. Once Identity has commands and queries, call
    /// this next to <c language="text">AddCratisArc</c>.
    /// </summary>
    /// <typeparam name="TBuilder">The type of the host application builder.</typeparam>
    /// <param name="builder">The <typeparamref name="TBuilder"/>.</param>
    /// <returns>The <typeparamref name="TBuilder"/> for continuation.</returns>
    public static TBuilder AddCratisIdentity<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddCratisIdentity(builder.Configuration, builder.Environment);
        return builder;
    }
}

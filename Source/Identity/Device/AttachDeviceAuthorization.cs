// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.Device;

/// <summary>Does not create a subjectless grant when a device initiates a request.</summary>
/// <param name="applications">The application manager.</param>
/// <param name="authorizations">The authorization manager.</param>
internal sealed class AttachDeviceAuthorization(IOpenIddictApplicationManager applications, IOpenIddictAuthorizationManager authorizations) : IOpenIddictServerHandler<ProcessSignInContext>
{
    /// <summary>Gets the replacement descriptor, retaining stock storage filters and order.</summary>
    internal static OpenIddictServerHandlerDescriptor Descriptor { get; } = OpenIddictServerHandlerDescriptor.CreateBuilder<ProcessSignInContext>()
        .AddFilter<OpenIddictServerHandlerFilters.RequireDegradedModeDisabled>()
        .AddFilter<OpenIddictServerHandlerFilters.RequireAuthorizationStorageEnabled>()
        .UseScopedHandler<AttachDeviceAuthorization>()
        .SetOrder(OpenIddictServerHandlers.AttachAuthorization.Descriptor.Order)
        .SetType(OpenIddictServerHandlerType.Custom).Build();

    /// <inheritdoc/>
    public ValueTask HandleAsync(ProcessSignInContext context) => context.EndpointType == OpenIddictServerEndpointType.DeviceAuthorization
        ? ValueTask.CompletedTask
        : new OpenIddictServerHandlers.AttachAuthorization(applications, authorizations).HandleAsync(context);
}

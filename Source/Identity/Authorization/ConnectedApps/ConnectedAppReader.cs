// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;
using OpenIddict.Abstractions;

namespace Cratis.Identity.Authorization.ConnectedApps;

/// <summary>Reads display metadata from the realm's operational authorization and application managers.</summary>
public sealed class ConnectedAppReader
{
    const int Limit = 1000;
    readonly IOpenIddictAuthorizationManager _authorizations;
    readonly IOpenIddictApplicationManager _applications;
    readonly IFenceStore _fences;

    /// <summary>Initializes a new instance of the <see cref="ConnectedAppReader"/> class.</summary>
    /// <param name="authorizations">The realm-bound authorization manager.</param>
    /// <param name="applications">The realm-bound application manager.</param>
    /// <param name="fences">The committed grant fences.</param>
    internal ConnectedAppReader(IOpenIddictAuthorizationManager authorizations, IOpenIddictApplicationManager applications, IFenceStore fences)
    {
        _authorizations = authorizations;
        _applications = applications;
        _fences = fences;
    }

    /// <summary>Returns display metadata only, propagating operational and fence-read failures.</summary>
    /// <param name="subject">The validated session account subject.</param>
    /// <param name="cancellationToken">Request cancellation.</param>
    /// <returns>Up to 1000 active connected apps.</returns>
    internal async Task<IEnumerable<ConnectedApp>> Read(string subject, CancellationToken cancellationToken)
    {
        List<ConnectedApp> apps = [];
        await foreach (var authorization in _authorizations.FindBySubjectAsync(subject, cancellationToken))
        {
            if (!await _authorizations.HasStatusAsync(authorization, OpenIddictConstants.Statuses.Valid, cancellationToken)) continue;
            var id = await _authorizations.GetIdAsync(authorization, cancellationToken);
            if (string.IsNullOrEmpty(id) || (await _fences.Read(id, cancellationToken))?.Fence is not null) continue;
            var app = await Describe(authorization, id, cancellationToken);
            if (app is not null) apps.Add(app);
        }
        return apps.OrderByDescending(app => app.GrantedAt).ThenBy(app => app.AuthorizationId, StringComparer.Ordinal).Take(Limit).ToArray();
    }

    async Task<ConnectedApp?> Describe(object authorization, string id, CancellationToken cancellationToken)
    {
        var applicationId = await _authorizations.GetApplicationIdAsync(authorization, cancellationToken);
        var application = string.IsNullOrEmpty(applicationId) ? null : await _applications.FindByIdAsync(applicationId, cancellationToken);
        if (application is null) return null;
        var clientId = await _applications.GetClientIdAsync(application, cancellationToken);
        if (string.IsNullOrEmpty(clientId)) return null;
        var displayName = await _applications.GetDisplayNameAsync(application, cancellationToken);
        var scopes = await _authorizations.GetScopesAsync(authorization, cancellationToken);
        var createdAt = await _authorizations.GetCreationDateAsync(authorization, cancellationToken);
        return new(id, clientId, string.IsNullOrWhiteSpace(displayName) ? clientId : displayName, scopes.ToArray(), createdAt ?? DateTimeOffset.MinValue);
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;
using OpenIddict.Abstractions;

namespace Cratis.Identity.Authorization.ConnectedApps;

/// <summary>Reads display metadata from the realm's operational authorization and application managers.</summary>
internal sealed class ConnectedAppReader
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

    /// <summary>Scans active grants and describes only the newest candidates, propagating operational and fence-read failures.</summary>
    /// <param name="subject">The validated session account subject.</param>
    /// <param name="cancellationToken">Request cancellation.</param>
    /// <returns>Up to 1000 active connected apps ordered by grant time descending, then authorization ID ordinal.</returns>
    internal async Task<IEnumerable<ConnectedApp>> Read(string subject, CancellationToken cancellationToken)
    {
        List<(object Authorization, string Id, DateTimeOffset GrantedAt)> candidates = [];
        await foreach (var authorization in _authorizations.FindBySubjectAsync(subject, cancellationToken))
        {
            if (!await _authorizations.HasStatusAsync(authorization, OpenIddictConstants.Statuses.Valid, cancellationToken)) continue;
            var id = await _authorizations.GetIdAsync(authorization, cancellationToken);
            if (string.IsNullOrEmpty(id) || (await _fences.Read(id, cancellationToken))?.Fence is not null) continue;
            var createdAt = await _authorizations.GetCreationDateAsync(authorization, cancellationToken);
            candidates.Add((authorization, id, createdAt ?? DateTimeOffset.MinValue));
        }
        List<ConnectedApp> apps = [];
        foreach (var candidate in candidates.OrderByDescending(candidate => candidate.GrantedAt).ThenBy(candidate => candidate.Id, StringComparer.Ordinal))
        {
            var app = await Describe(candidate.Authorization, candidate.Id, candidate.GrantedAt, cancellationToken);
            if (app is not null) apps.Add(app);
            if (apps.Count == Limit) break;
        }
        return apps.ToArray();
    }

    async Task<ConnectedApp?> Describe(object authorization, string id, DateTimeOffset grantedAt, CancellationToken cancellationToken)
    {
        var applicationId = await _authorizations.GetApplicationIdAsync(authorization, cancellationToken);
        var application = string.IsNullOrEmpty(applicationId) ? null : await _applications.FindByIdAsync(applicationId, cancellationToken);
        if (application is null) return null;
        var clientId = await _applications.GetClientIdAsync(application, cancellationToken);
        if (string.IsNullOrEmpty(clientId)) return null;
        var displayName = await _applications.GetDisplayNameAsync(application, cancellationToken);
        var scopes = await _authorizations.GetScopesAsync(authorization, cancellationToken);
        return new(id, clientId, string.IsNullOrWhiteSpace(displayName) ? clientId : displayName, scopes.ToArray(), grantedAt);
    }
}

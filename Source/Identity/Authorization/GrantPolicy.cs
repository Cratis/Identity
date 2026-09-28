// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Resources;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.Authorization;

/// <summary>
/// Represents an implementation of <see cref="IGrantPolicy"/>.
/// </summary>
/// <remarks>
/// Every grant is bound to exactly one resource server, so every access token carries exactly one audience.
/// The resource server comes from the RFC 8707 <c language="text">resource</c> parameter; when a client sends none, it is
/// inferred from the requested scopes, but only when they point at a single resource server. Requested scopes
/// that belong to another resource server are dropped rather than granted.
/// </remarks>
/// <param name="resources">The <see cref="IResourceCatalog"/>.</param>
public class GrantPolicy(IResourceCatalog resources) : IGrantPolicy
{
    /// <inheritdoc/>
    public GrantEvaluation Evaluate(GrantRequest request)
    {
        var scopes = request.Scopes.Distinct(StringComparer.Ordinal).ToArray();
        var indicators = request.ResourceIndicators.Distinct(StringComparer.Ordinal).ToArray();

        var resolution = indicators.Length > 0 ? ResolveFromIndicators(indicators) : ResolveFromScopes(scopes);
        if (resolution is NotGrantable notGrantable)
        {
            return notGrantable;
        }

        var resource = ((ResolvedResource)resolution).Resource;
        var resourceScopes = scopes.Where(resource.Owns).ToArray();
        if (resourceScopes.Length == 0)
        {
            return new NotGrantable(Errors.InvalidScope, $"None of the requested scopes apply to the resource \"{resource.Audience}\".");
        }

        return new Grantable(new Grant(resource, [.. resourceScopes, .. scopes.Where(ProtocolScopes.Contains)]));
    }

    /// <inheritdoc/>
    public NotGrantable? EvaluateTokenRequest(IEnumerable<string> resourceIndicators, IEnumerable<string> grantedAudiences)
    {
        var granted = grantedAudiences.ToHashSet(StringComparer.Ordinal);
        var outside = resourceIndicators.Where(_ => resources.FindByIndicator(_) is not { } resource || !granted.Contains(resource.Audience));

        return outside.Any()
            ? new NotGrantable(Errors.InvalidTarget, "The requested resource is not the resource this grant was authorized for.")
            : null;
    }

    GrantEvaluation ResolveFromIndicators(string[] indicators)
    {
        var resolved = indicators.Select(resources.FindByIndicator).ToArray();
        if (resolved.Any(_ => _ is null))
        {
            return new NotGrantable(Errors.InvalidTarget, "The requested resource is not known.");
        }

        var audiences = resolved.Select(_ => _!.Audience).Distinct(StringComparer.Ordinal).ToArray();
        return audiences.Length == 1
            ? new ResolvedResource(resolved[0]!)
            : new NotGrantable(Errors.InvalidTarget, "A grant can only be bound to one resource server; request each resource separately.");
    }

    GrantEvaluation ResolveFromScopes(string[] scopes)
    {
        var candidates = scopes
            .Where(_ => !ProtocolScopes.Contains(_))
            .SelectMany(resources.FindByScope)
            .DistinctBy(_ => _.Audience, StringComparer.Ordinal)
            .ToArray();

        return candidates.Length == 1
            ? new ResolvedResource(candidates[0])
            : new NotGrantable(Errors.InvalidTarget, "The resource parameter is required to determine which resource server the grant is for.");
    }

    sealed record ResolvedResource(ResourceServer Resource) : GrantEvaluation;
}

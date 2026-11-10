// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.Clients;

/// <summary>
/// Validates client registrations without writing configuration or protocol state.
/// </summary>
internal static class ClientRegistrationRules
{
    /// <summary>
    /// Validates the client descriptor against the server's enabled challenge methods.
    /// </summary>
    /// <param name="descriptor">The proposed client descriptor.</param>
    /// <param name="codeChallengeMethods">The enabled PKCE challenge methods.</param>
    /// <param name="allowLocalhost">Whether a native client may also register the <c language="text">localhost</c> host. Only the local-development profile's clients pass <see langword="true"/>.</param>
    /// <returns>The validation result, containing no credentials or redirect values.</returns>
    public static ClientRegistrationValidation Validate(OpenIddictApplicationDescriptor descriptor, IEnumerable<string> codeChallengeMethods, bool allowLocalhost = false)
    {
        var refusals = new List<string>();
        if (descriptor.ClientType == ClientTypes.Public &&
            (!descriptor.Requirements.Contains(Requirements.Features.ProofKeyForCodeExchange) ||
             !codeChallengeMethods.ToHashSet(StringComparer.Ordinal).SetEquals([CodeChallengeMethods.Sha256])))
        {
            refusals.Add("Public clients require PKCE with S256 only.");
        }

        if (descriptor.ClientType == ClientTypes.Confidential && string.IsNullOrWhiteSpace(descriptor.ClientSecret))
        {
            refusals.Add("Confidential clients require a secret.");
        }

        var native = descriptor.ClientType == ClientTypes.Public && descriptor.ApplicationType == ApplicationTypes.Native;
        if (descriptor.RedirectUris.Any(uri => !IsValidRedirect(uri, native, allowLocalhost)))
        {
            refusals.Add("Redirect URIs must be absolute and contain no user information or fragment; public native clients require an HTTP loopback literal.");
        }

        return new(refusals);
    }

    static bool IsValidRedirect(Uri uri, bool native, bool allowLocalhost) =>
        uri.IsAbsoluteUri && string.IsNullOrEmpty(uri.UserInfo) && string.IsNullOrEmpty(uri.Fragment) &&
        (!native || (uri.Scheme == Uri.UriSchemeHttp && IsNativeHost(uri.Host, allowLocalhost)));

    static bool IsNativeHost(string host, bool allowLocalhost) =>
        string.Equals(host, "127.0.0.1", StringComparison.Ordinal) || string.Equals(host, "[::1]", StringComparison.Ordinal) ||
        (allowLocalhost && string.Equals(host, "localhost", StringComparison.Ordinal));
}

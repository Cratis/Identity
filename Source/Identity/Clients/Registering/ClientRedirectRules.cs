// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Clients;

namespace Cratis.Identity.Clients.Registering;

/// <summary>Strict redirect admission for Internal client commands, without changing legacy descriptors.</summary>
internal static class ClientRedirectRules
{
    /// <summary>Parses exact HTTPS web redirects or HTTP native loopback IP literals.</summary>
    /// <param name="values">The proposed allowlist.</param>
    /// <param name="applicationType">The accepted application type.</param>
    /// <param name="redirects">The parsed allowlist, containing no request-time URI.</param>
    /// <returns>Whether every entry is admitted.</returns>
    public static bool TryParse(string[]? values, ClientApplicationType applicationType, out Uri[] redirects)
    {
        redirects = [];
        if (values is null)
        {
            return false;
        }

        List<Uri> parsed = [];
        foreach (var value in values)
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || !IsSafe(uri, applicationType))
            {
                return false;
            }

            parsed.Add(uri);
        }

        redirects = parsed.ToArray();
        return true;
    }

    static bool IsSafe(Uri uri, ClientApplicationType applicationType)
    {
        if (string.IsNullOrEmpty(uri.Host) || !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Fragment))
        {
            return false;
        }

        return applicationType == ClientApplicationType.Native ? IsNativeLoopback(uri) : uri.Scheme == Uri.UriSchemeHttps;
    }

    static bool IsNativeLoopback(Uri uri) => uri.Scheme == Uri.UriSchemeHttp && IsLoopbackLiteral(uri.Host);

    static bool IsLoopbackLiteral(string host) => string.Equals(host, "127.0.0.1", StringComparison.Ordinal) || string.Equals(host, "[::1]", StringComparison.Ordinal);
}

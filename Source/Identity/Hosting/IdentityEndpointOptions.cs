// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Hosting;

/// <summary>
/// Defines Identity's endpoint paths. Configure the prefix through Identity:Endpoints:RoutePrefix before registration.
/// </summary>
public class IdentityEndpointOptions
{
    static readonly string[] _reservedPrefixes = ["/.well-known", "/signin-github", "/healthz", "/readyz"];

    /// <summary>
    /// Gets or sets the prefix for protocol and optional sign-out routes and generated API realm matching. Discovery and the GitHub callback remain at the root.
    /// </summary>
    public PathString RoutePrefix { get; set; }

    /// <summary>
    /// Gets or sets whether to map the optional antiforgery-protected sign-out form. Defaults to false.
    /// </summary>
    public bool MapSignOut { get; set; }

    /// <summary>Checks paths for ambiguous encodings and traversal segments.</summary>
    /// <param name="path">The request or prefix path.</param>
    /// <returns>Whether the path has unsafe segments.</returns>
    internal static bool HasUnsafeSegments(string path) =>
        path.Contains('%') || path.Contains('\\') || path.Contains("//", StringComparison.Ordinal) ||
        path.Split('/').Any(segment => string.Equals(segment, ".", StringComparison.Ordinal) || string.Equals(segment, "..", StringComparison.Ordinal));

    /// <summary>Refuses ambiguous prefixes and collisions with host or discovery routes.</summary>
    /// <exception cref="ArgumentException">The route prefix is not a supported path.</exception>
    internal void Validate()
    {
        var prefix = RoutePrefix.Value ?? string.Empty;
        if (prefix.Length == 0)
        {
            return;
        }

        if (!HasValidShape(prefix) || IsReservedPrefix())
        {
            throw new ArgumentException("Identity:Endpoints:RoutePrefix must be empty or a path of at most 128 ASCII unreserved characters, beginning with one slash, with no trailing slash, empty or dot segments, escapes, or reserved endpoint prefix.");
        }
    }

    static bool HasValidShape(string prefix) =>
        prefix.Length <= 128 && prefix[0] == '/' && !prefix.EndsWith('/') &&
        !HasUnsafeSegments(prefix) && prefix.All(IsPathCharacter);

    static bool IsPathCharacter(char character) => char.IsAsciiLetterOrDigit(character) || character is '/' or '-' or '_' or '.' or '~';

    bool IsReservedPrefix() => _reservedPrefixes
        .Any(reserved => RoutePrefix.StartsWithSegments(reserved, StringComparison.OrdinalIgnoreCase));
}

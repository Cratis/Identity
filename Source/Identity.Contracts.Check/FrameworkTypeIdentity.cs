// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Identity.Contracts.Check;

/// <summary>Removes runtime version churn without discarding the rest of a CLR type identity.</summary>
internal static partial class FrameworkTypeIdentity
{
    [GeneratedRegex(@"(?<assembly>, (?:System|Microsoft)\.[^,\[\]]+), Version=[^,\[\]]+", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex FrameworkAssemblyVersion { get; }

    /// <summary>Ignores only framework assembly versions, including those in nested generic arguments.</summary>
    /// <param name="typeName">The CLR type identity, without loading its assemblies.</param>
    /// <returns>The type identity with framework assembly versions omitted.</returns>
    internal static string Canonicalize(string typeName) => FrameworkAssemblyVersion.Replace(typeName, "${assembly}");
}

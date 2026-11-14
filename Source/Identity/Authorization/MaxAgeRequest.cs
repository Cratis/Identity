// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Microsoft.Extensions.Primitives;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.Authorization;

/// <summary>
/// Parses the explicit maximum-age parameter of an authorization request from the raw HTTP request.
/// The lenient value bound by the OpenID Connect server is not trusted, so malformed input is refused.
/// </summary>
internal static class MaxAgeRequest
{
    /// <summary>
    /// Checks the query and, when applicable, the posted form for the parameter.
    /// </summary>
    /// <param name="request">The current HTTP request, if available.</param>
    /// <returns>Whether the parameter is absent, valid or invalid.</returns>
    internal static async ValueTask<MaxAgeState> Evaluate(HttpRequest? request)
    {
        if (request is null) return MaxAgeState.Absent;

        var occurrences = Occurrences(request.Query);
        if (HttpMethods.IsPost(request.Method) && request.HasFormContentType)
        {
            var form = await request.ReadFormAsync(request.HttpContext.RequestAborted);
            occurrences = [.. occurrences, .. Occurrences(form)];
        }

        if (occurrences.Count == 0) return MaxAgeState.Absent;
        return occurrences.Count == 1 && IsValid(occurrences[0]) ? MaxAgeState.Valid : MaxAgeState.Invalid;
    }

    static List<KeyValuePair<string, string?>> Occurrences(IEnumerable<KeyValuePair<string, StringValues>> source) =>
        [.. source
            .Where(_ => string.Equals(_.Key, Parameters.MaxAge, StringComparison.OrdinalIgnoreCase))
            .SelectMany(_ => _.Value.DefaultIfEmpty(string.Empty).Select(value => new KeyValuePair<string, string?>(_.Key, value)))];

    static bool IsValid(KeyValuePair<string, string?> occurrence) =>
        string.Equals(occurrence.Key, Parameters.MaxAge, StringComparison.Ordinal)
        && long.TryParse(occurrence.Value, NumberStyles.None, CultureInfo.InvariantCulture, out _);
}

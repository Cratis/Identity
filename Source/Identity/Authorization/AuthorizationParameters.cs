// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Primitives;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.Authorization;

/// <summary>
/// Extracts the OAuth parameters of an authorization request so they can be carried through upstream login
/// and the consent form unchanged.
/// </summary>
internal static class AuthorizationParameters
{
    /// <summary>
    /// Gets the names of the consent form fields, which are not OAuth parameters.
    /// </summary>
    public static IEnumerable<string> ConsentFields { get; } = [ConsentForm.Decision, ConsentForm.Tenant, ConsentForm.AntiforgeryField];

    /// <summary>
    /// Gets the OAuth parameters of a request, excluding consent form fields. Repeated parameters (such as
    /// <c language="text">resource</c>) are kept.
    /// </summary>
    /// <param name="request">The <see cref="HttpRequest"/>.</param>
    /// <returns>The parameters as name and value pairs.</returns>
    public static IEnumerable<KeyValuePair<string, string>> From(HttpRequest request)
    {
        IEnumerable<KeyValuePair<string, StringValues>> source =
            HttpMethods.IsPost(request.Method) && request.HasFormContentType ? request.Form : request.Query;

        return source
            .Where(_ => !ConsentFields.Contains(_.Key, StringComparer.Ordinal))
            .SelectMany(_ => _.Value.OfType<string>().Select(value => new KeyValuePair<string, string>(_.Key, value)))
            .ToArray();
    }

    /// <summary>
    /// Gets the parameters to return to after upstream login. A <c language="text">login</c> prompt has been satisfied by the
    /// login itself, so it is removed; keeping it would send the user back to login forever.
    /// </summary>
    /// <param name="parameters">The parameters of the authorization request.</param>
    /// <returns>The parameters to return with.</returns>
    public static IEnumerable<KeyValuePair<string, string>> AfterLogin(IEnumerable<KeyValuePair<string, string>> parameters) =>
        parameters
            .Select(_ => string.Equals(_.Key, Parameters.Prompt, StringComparison.Ordinal)
                ? new KeyValuePair<string, string>(_.Key, string.Join(' ', _.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(p => !string.Equals(p, PromptValues.Login, StringComparison.Ordinal))))
                : _)
            .Where(_ => !string.Equals(_.Key, Parameters.Prompt, StringComparison.Ordinal) || _.Value.Length > 0)
            .ToArray();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Authorization;

/// <summary>Recognizes explicit consent decisions without treating an authorization GET as approval.</summary>
internal static class ConsentSubmission
{
    /// <summary>Checks whether the request submits a consent decision.</summary>
    /// <param name="context">The current request.</param>
    /// <returns>Whether a form POST contains the decision field.</returns>
    public static bool IsSubmitted(HttpContext context) =>
        HttpMethods.IsPost(context.Request.Method) && context.Request.HasFormContentType && context.Request.Form.ContainsKey(ConsentForm.Decision);
}

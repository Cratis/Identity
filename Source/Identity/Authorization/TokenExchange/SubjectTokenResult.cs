// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Authorization.TokenExchange;

/// <summary>
/// Represents the outcome of validating a subject token.
/// </summary>
/// <param name="Subject">The verified subject, when accepted.</param>
/// <param name="Refusal">The refusal category, or <see cref="SubjectTokenRefusal.None"/> when accepted.</param>
internal sealed record SubjectTokenResult(ValidatedSubject? Subject, SubjectTokenRefusal Refusal)
{
    /// <summary>
    /// Creates an accepted result.
    /// </summary>
    /// <param name="subject">The <see cref="ValidatedSubject"/>.</param>
    /// <returns>The result.</returns>
    public static SubjectTokenResult Accepted(ValidatedSubject subject) => new(subject, SubjectTokenRefusal.None);

    /// <summary>
    /// Creates a refused result.
    /// </summary>
    /// <param name="refusal">The refusal category.</param>
    /// <returns>The result.</returns>
    public static SubjectTokenResult Refused(SubjectTokenRefusal refusal) => new(null, refusal);
}
